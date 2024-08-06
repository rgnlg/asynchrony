using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using Oculus.Interaction;
using UnityEngine;

[System.Serializable]
public class UserData
{
    public QuestionAnswer[] answers;
    
    public bool test;

    public string device;
    public string IP;
    
    public long start;
    public long end;

    public string condition;

    public QuestionAnswer[] timestamps;

    public UserData(List<QuestionAnswer> answers, bool isTest, string condition, Dictionary<string, int> timestamps)
    {
        this.test = isTest;
        this.condition = condition;

        List<QuestionAnswer> timestampsList = new List<QuestionAnswer>();

        foreach(KeyValuePair<string, int> ts in timestamps) {
            timestampsList.Add(new QuestionAnswer(ts.Key, ts.Value));
        } 

        this.timestamps = timestampsList.ToArray();

        this.answers = answers.ToArray();
        
        this.setDeviceInfo();
        this.setIP();
    }

    private long timestamp()
    {
        return new System.DateTimeOffset(DateTime.Now).ToUnixTimeSeconds();
    }

    public void setIP()
    {
        if(!this.test)
        {
            this.IP = new WebClient().DownloadString("http://icanhazip.com").Trim();
        }
        else {
            this.IP = "localhost";
        }
    }

    public void setDeviceInfo()
    {
        this.device = SystemInfo.deviceModel;
    }

    public void setStart(int start)
    {
        this.start = start;
    }

    public void setEnd()
    {
        this.end = this.timestamp();
    }
}

[System.Serializable]
public class QuestionAnswer
{
    public string key;
    public int value;

    public QuestionAnswer(string key, int value)
    {
        this.key = key;
        this.value = value;
    }
}

