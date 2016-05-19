//------------------------------------------------------------------------------
// Copyright 2016 Proletariat Inc. All rights reserved.
//------------------------------------------------------------------------------

using System;

namespace SMTV {

  [Serializable]
  public class User {
      public string id;
      public string token;
  }

  [Serializable]
  public class Channel {
      public string id;
      public string active_poll;
      public string active_wager;
  }

  [Serializable]
  public class PollCreateBody {
    public string[] options;
  }

  [Serializable]
  public class Poll {
      public string id;
      public bool closed;
      public int update_stamp;
      public string[] options;
      public int[] votes;
  }

  [Serializable]
  public class PollVote {
    public int update_stamp;
    public PollVoteEntry[] votes;
  }

  [Serializable]
  public class PollVoteEntry {
    public string user_id;
    public int option;
  }

  [Serializable]
  public class WagerCreateBody {
    public string[] options;
    public int betting_duration;
  }

  [Serializable]
  public class Wager {
      public string id;
      public int update_stamp;
      public string state;
      public WagerEntry[] entries;
  }

  [Serializable]
  public class WagerEntry {
      public string option;
      public int score;
      public int total;
      public string state;
  }

  [Serializable]
  public class WagerScoreBody {
    public WagerEntry[] entries;
  }

  [Serializable]
  public class ModelResponse {
    public User[] users;
    public Channel[] channels;
    public Poll[] polls;
    public Wager[] wagers;
  }
}