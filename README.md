# TwitterScriptWithMedia

使い方

//TwitterScriptのメンバ変数を宣言。GetComponentするなりアタッチするなりしてください

public TwitterScript tws;


//PINコードを発行する画面を開く。

tws.IssuedPIN();


//PINコードをTwitterAPIに送る。

string pin = "";

tws.SendPIN(string pin);


//ツイートする。

string text = "hoge";

tws.PostTweet(text)


//画像付きツイートをする

string text;

Sprite sprite;

tws.ImageTweet(text, sprite);


string text;

string mediaPath;

tws.ImageTweet(text, mediaPath);
