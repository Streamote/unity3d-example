//------------------------------------------------------------------------------
// Copyright 2016 Proletariat Inc. All rights reserved.
//------------------------------------------------------------------------------

using UnityEngine;
using UnityEngine.Experimental.Networking;
using System.Collections;
using System.Linq;
using System;
using LitJson;

namespace SMTV {

  public static class API {
    public static string BaseURL = "https://api.streamote.tv/v1";
    public static string ClientID = "example";
  }

  public class Request<T> {
    UnityWebRequest wr;
    T resp;

    public Action<string> OnError;
    public Action<T> OnSuccess;

    public bool isError { get { return wr == null || wr.isError; } }
    public string error { get { return wr.error; } }
    public T response { get { return resp; } }

    public static Request<T> Create(params System.Object[] parts) {
      var r = new Request<T>();
      var strs = parts.Select(v => v.ToString()).ToArray();
      r.wr = UnityWebRequest.Get(API.BaseURL + string.Join("", strs));
      r.wr.SetRequestHeader("Client-ID", API.ClientID);
      return r;
    }

    public Request<T> Auth(string token) {
      this.wr.SetRequestHeader("Authorization", "Bearer " + token);
      return this;
    }

    public Request<T> Post() {
      this.wr.method = "POST";
      return this;
    }

    public Request<T> Post(System.Object json) {
      var body = System.Text.Encoding.UTF8.GetBytes(JsonMapper.ToJson(json));
      this.wr.method = "POST";
      this.wr.uploadHandler  = new UploadHandlerRaw(body);
      this.wr.uploadHandler.contentType = "application/json";
      return this;
    }

    public IEnumerator Send() {
      yield return this.wr.Send();

      if(wr.isError) {
        if (this.OnError != null) {
          this.OnError(wr.error);
        }
      }
      else {
        this.resp = JsonMapper.ToObject<T>(this.wr.downloadHandler.text);
        if (this.OnSuccess != null) {
          this.OnSuccess(this.resp);
        }
      }
    }
  }
}
