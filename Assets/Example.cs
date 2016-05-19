//------------------------------------------------------------------------------
// Copyright 2016 Proletariat Inc. All rights reserved.
//------------------------------------------------------------------------------

using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Linq;

// Handy typedef
using SMTV;
using ModelRequest = SMTV.Request<SMTV.ModelResponse>;

public class Example : MonoBehaviour {
  Channel myChannel;
  Poll myPoll;
  Wager myWager;

  public string apiURL = SMTV.API.BaseURL;
  public string clientID = SMTV.API.ClientID;
  public string authToken;

  void Start () {
    if (!string.IsNullOrEmpty(this.apiURL)) {
      SMTV.API.BaseURL = this.apiURL;
    }

    if (!string.IsNullOrEmpty(this.clientID)) {
      SMTV.API.ClientID = this.clientID;
    }

    if (!string.IsNullOrEmpty(this.authToken)) {
      StartCoroutine(GetBroadcaster());
    }
  }

  // The /user endpoint returns state for the authorized
  IEnumerator GetBroadcaster() {
    var req = ModelRequest.Create("/channel").Auth(this.authToken);
    req.OnError += Debug.Log;
    req.OnSuccess += (ModelResponse mr) => {
      this.myChannel = mr.channels[0];

      if (!string.IsNullOrEmpty(myChannel.active_poll)) {
        UpdatePoll(mr);
      }

      if (!string.IsNullOrEmpty(myChannel.active_wager)) {
        UpdateWager(mr);
      }

      GameObject.Find("Broadcaster").GetComponent<Text>().text = this.myChannel.id;
    };

    yield return req.Send();
  }

  // This is an example of long-polling login using an external browser.
  // If you were using a browser plugin, the long polling call (/auth/callback) and guid aren't required.
  // Instead you could intercept the redirect to http://streamote.tv/callback?token=<access_token>
  // and use the access token provided
  IEnumerator BrowserLogin() {
    var guid = System.Guid.NewGuid().ToString();
    Application.OpenURL(SMTV.API.BaseURL + "/auth/twitch?track=" + guid);

    var req = ModelRequest.Create("/auth/complete?track=", guid);
    req.OnError += Debug.Log;
    req.OnSuccess += (ModelResponse mr) => {
      this.authToken = req.response.users[0].token;
      StartCoroutine(GetBroadcaster());
    };

    yield return req.Send();
  }

  void UpdateWager(ModelResponse mr) {
    this.myWager = mr.wagers[0];
    GameObject.Find("Wager").GetComponent<Text>().text = this.myWager.id;
  }

  IEnumerator CreateWager(string[] options) {
    var wc = new WagerCreateBody();
    wc.options = options;
    wc.betting_duration = 0;

    var req = ModelRequest.Create("/wagers/create").Auth(this.authToken).Post(wc);
    req.OnError += Debug.Log;
    req.OnSuccess += UpdateWager;

    yield return req.Send();
  }

  IEnumerator WagerScore() {
    if (this.myWager == null || this.myWager.state == "closing" || this.myWager.state == "closed") {
      Debug.Log("No Wager Started");
    }
    else {
      ModelRequest req;

      // If we are still in the betting phase, end it
      if (this.myWager.state == "betting") {
        req = ModelRequest.Create("/wagers/", this.myWager.id, "/betting/close").Auth(this.authToken).Post();
        req.OnError += Debug.Log;
        req.OnSuccess += UpdateWager;

        yield return req.Send();
      }

      var wc = new WagerScoreBody();
      wc.entries = (WagerEntry[]) this.myWager.entries.Clone();

      foreach (var we in wc.entries) {
        var i = System.Array.IndexOf(this.myPoll.options, we.option);
        if (i >= 0) {
          we.score = this.myPoll.votes[i];
        }
      }

      req = ModelRequest.Create("/wagers/", this.myWager.id, "/score").Auth(this.authToken).Post(wc);
      req.OnError += Debug.Log;
      req.OnSuccess += UpdateWager;

      yield return req.Send();
    }
  }

  IEnumerator CloseWager() {
    if (this.myWager == null || this.myWager.state == "closing" || this.myWager.state == "closed") {
      Debug.Log("No Active Wager Found");
    }
    else {
      var req = ModelRequest.Create("/wagers/", this.myWager.id, "/close").Auth(this.authToken).Post();
      req.OnError += Debug.Log;
      req.OnSuccess += UpdateWager;

      yield return req.Send();
    }
  }

  IEnumerator CreatePoll(string[] options) {

    var pc = new PollCreateBody();
    pc.options = options;

    var req = ModelRequest.Create("/polls/create").Auth(this.authToken).Post(pc);
    req.OnError += Debug.Log;
    req.OnSuccess += UpdatePoll;

    yield return req.Send();
  }

  IEnumerator ClosePoll() {
    if (this.myPoll == null || this.myPoll.closed) {
      Debug.Log("No Active Poll Found");
    }
    else {
      var req = ModelRequest.Create("/polls/", this.myPoll.id, "/close").Auth(this.authToken).Post();
      req.OnError += Debug.Log;
      req.OnSuccess += UpdatePoll;

      yield return req.Send();
    }
  }

  void UpdatePoll(ModelResponse mr) {
    var sameID = this.myPoll != null && mr.polls[0].id == this.myPoll.id;

    this.myPoll = mr.polls[0];
    GameObject.Find("Poll").GetComponent<Text>().text = this.myPoll.id;

    if (!sameID) {
      StartCoroutine(StartVoteListener(this.myPoll.id));
    }
  }

  IEnumerator StartVoteListener(string id) {
    var updateStamp = 0;
    var voters = GameObject.Find("Poll Voters").GetComponent<Text>();
    var votes = GameObject.Find("Poll Votes").GetComponent<Text>();
    voters.text = votes.text = "";

    while (this.myPoll != null && this.myPoll.id == id) {

      var req = Request<PollVote>.Create("/polls/", id, "/votes?update_stamp=", updateStamp);
      req.OnError += Debug.Log;
      req.OnSuccess += (PollVote v) => {
        if (this.myPoll == null || this.myPoll.id != id) {
          return;
        }
        else if (updateStamp != req.response.update_stamp) {
          updateStamp = req.response.update_stamp;

          var i = 0;
          var vrs = new string[v.votes.Length];
          var vs = new string[v.votes.Length];
          foreach (var pve in v.votes) {
            vrs[i] = string.Format("{0} ", pve.user_id);
            vs[i] = "" + pve.option;
            i++;
          }

          voters.text = string.Join("\n", vrs);
          votes.text = string.Join("\n", vs);
        }
      };
      
      yield return req.Send();
    }
  }

  IEnumerator StartGame() {
    var options = new string[]{"a", "b", "c"};
    yield return CreatePoll(options);
    yield return CreateWager(options);
  }

  IEnumerator StopGame() {
    yield return ClosePoll();
    yield return WagerScore();
    yield return CloseWager();
  }

  public void OnAuthClick() {
    StartCoroutine(BrowserLogin());
  }

  public void OnStartClick() {
    StartCoroutine(StartGame());
  }

  public void OnStopClick() {
    StartCoroutine(StopGame());
  }
}