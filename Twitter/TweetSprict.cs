
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TweetSprict : MonoBehaviour
{





    //アプリケーションの選別に使うコードです
    // Twitterアプリケーションをまだ作ってない人はここから設定をしてください http://dev.twitter.com/apps/new
    public string CONSUMER_KEY = "LSS9vTXPJazjoUmIbGGqRchWi";
    public string CONSUMER_SECRET = "TcVU4mTRXZFpeKElVlhxowz1fL71exokQ7SUFKOdxZfoKpioC9";


    //ユーザー情報をここに格納します（編集しなくてもよいです）
    const string PLAYER_PREFS_TWITTER_USER_ID = "TwitterUserID";
    const string PLAYER_PREFS_TWITTER_USER_SCREEN_NAME = "TwitterUserScreenName";
    const string PLAYER_PREFS_TWITTER_USER_TOKEN = "TwitterUserToken";
    const string PLAYER_PREFS_TWITTER_USER_TOKEN_SECRET = "TwitterUserTokenSecret";

    //Twitter.csからResponseを呼び出すためのものです
    Twitter.RequestTokenResponse m_RequestTokenResponse;
    Twitter.AccessTokenResponse m_AccessTokenResponse;
      
    // 127行目のメソッドを起動します
    void Start()
    {

        LoadTwitterUserInfo();
    }
    


    private string _Tweet;
    

    //PIN発行の申請を行います
    public void IssuedPIN()
    {
        if (string.IsNullOrEmpty(CONSUMER_KEY) || string.IsNullOrEmpty(CONSUMER_SECRET))
        {

            Application.OpenURL("http://dev.twitter.com/apps/new");

        }
        else
        {
            StartCoroutine(Twitter.API.GetRequestToken(CONSUMER_KEY, CONSUMER_SECRET,
                                                           new Twitter.RequestTokenCallback(this.OnRequestTokenCallback)));
        }
    }

    //PINFieldにかかれた認証コードをTwitterに送信します
    public void SendPIN(string PIN)
    {

        //PINを送信します
        StartCoroutine(Twitter.API.GetAccessToken(CONSUMER_KEY, CONSUMER_SECRET, m_RequestTokenResponse.Token, PIN,
                          new Twitter.AccessTokenCallback(this.OnAccessTokenCallback)));
    }
    

    //ツイートをします
    public void Tweet(string Tweet)
    {
        _Tweet = Tweet;
        StartCoroutine(Twitter.API.PostTweet(Tweet, CONSUMER_KEY, CONSUMER_SECRET, m_AccessTokenResponse, new Twitter.PostTweetCallback(this.TweetCallBack)));

    }


    //パスから画像付きのツイートをします
    public void ImageTweet(string Tweet, string Path)
    {
        _Tweet = Tweet + "\n" + "画像パス : " + Path;
        //"file://C:/Users/UnityAndroid/Pictures/ScreenShot/ScreenShot.png"
        StartCoroutine(Twitter.API.PostTweetWithMedia(Tweet, Path, CONSUMER_KEY, CONSUMER_SECRET, m_AccessTokenResponse,
            new Twitter.PostTweetCallback(this.TweetCallBack)));

    }


    //Unity標準のuGUIのImageから画像に変換し直接ツイッターに投稿します

    public void ImageTweet(string Tweet, Image image)
    {
        StartCoroutine(Twitter.API.PostTweetConvertImage(Tweet, image, CONSUMER_KEY, CONSUMER_SECRET, m_AccessTokenResponse,
            new Twitter.PostTweetCallback(this.TweetCallBack)));
    }

    public void ImageTweet(string Tweet, Sprite sprite)
    {
        StartCoroutine(Twitter.API.PostTweetWithMedia(Tweet, sprite, CONSUMER_KEY, CONSUMER_SECRET, m_AccessTokenResponse,
            new Twitter.PostTweetCallback(this.TweetCallBack)));
    }


    //すでにログインしている場合、保存されたデータでユーザー認証を行います
    void LoadTwitterUserInfo()
    {
        m_AccessTokenResponse = new Twitter.AccessTokenResponse();

        m_AccessTokenResponse.UserId = PlayerPrefs.GetString(PLAYER_PREFS_TWITTER_USER_ID);
        m_AccessTokenResponse.ScreenName = PlayerPrefs.GetString(PLAYER_PREFS_TWITTER_USER_SCREEN_NAME);
        m_AccessTokenResponse.Token = PlayerPrefs.GetString(PLAYER_PREFS_TWITTER_USER_TOKEN);
        m_AccessTokenResponse.TokenSecret = PlayerPrefs.GetString(PLAYER_PREFS_TWITTER_USER_TOKEN_SECRET);

        if (!string.IsNullOrEmpty(m_AccessTokenResponse.Token) &&
            !string.IsNullOrEmpty(m_AccessTokenResponse.ScreenName) &&
            !string.IsNullOrEmpty(m_AccessTokenResponse.Token) &&
            !string.IsNullOrEmpty(m_AccessTokenResponse.TokenSecret))
        {
            string log = "LoadTwitterUserInfo - succeeded";
            log += "\n    UserId : " + m_AccessTokenResponse.UserId;
            log += "\n    ScreenName : " + m_AccessTokenResponse.ScreenName;
            log += "\n    Token : " + m_AccessTokenResponse.Token;
            log += "\n    TokenSecret : " + m_AccessTokenResponse.TokenSecret;
            print(log);
        }
    }


    //以下、コールバック。各処理が成功したかを返します（編集不可）
    void OnRequestTokenCallback(bool success, Twitter.RequestTokenResponse response)
    {
        if (success)
        {

            Debug.Log("認証コード発行画面を開きました。PIN入力欄に認証コードを書き、送信してください");
            m_RequestTokenResponse = response;

            Twitter.API.OpenAuthorizationPage(response.Token);
        }
        else
        {
            Debug.Log("認証コード発行画面を開けませんでした。時間をおいてもう一度お試しください");
        }

    }

    void OnAccessTokenCallback(bool success, Twitter.AccessTokenResponse response)
    {
        if (success)
        {
            string log = "アカウントを認証しました";
            log += "\n" + response.ScreenName + "さん";

            Debug.Log(log);


            m_AccessTokenResponse = response;

            PlayerPrefs.SetString(PLAYER_PREFS_TWITTER_USER_ID, response.UserId);
            PlayerPrefs.SetString(PLAYER_PREFS_TWITTER_USER_SCREEN_NAME, response.ScreenName);
            PlayerPrefs.SetString(PLAYER_PREFS_TWITTER_USER_TOKEN, response.Token);
            PlayerPrefs.SetString(PLAYER_PREFS_TWITTER_USER_TOKEN_SECRET, response.TokenSecret);


        }
        else
        {
            Debug.Log("アカウントの認証に失敗しました。PINコードが正しいか確認してください");
            //AcceceSuccess.text = "アカウントの認証に失敗しました";
        }
    }


    void TweetCallBack(bool success)
    {
        print("OnPostTweet - " + (success ? "succedded." : "failed."));
        if (success)
        {
            Debug.Log("ツイートに成功しました\n" + _Tweet);

        }



    }


    //void OnPostTweet(bool success, string response)

    //print("OnPostTweet - " + (success ? "succedded." : "failed."));

    // if (success)

    //var json = JSON.Parse(response);

    //print(json["id"]);
}



