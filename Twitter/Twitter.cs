
using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Collections;
using System.Text.RegularExpressions;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using UnityEngine.UI;
using UnityEngine;

namespace Twitter
{
    public class RequestTokenResponse
    {
        public string Token { get; set; }
        public string TokenSecret { get; set; }
    }

    public class AccessTokenResponse
    {
        public string Token { get; set; }
        public string TokenSecret { get; set; }
        public string UserId { get; set; }
        public string ScreenName { get; set; }
    }

    public delegate void RequestTokenCallback(bool success, RequestTokenResponse response);
    public delegate void AccessTokenCallback(bool success, AccessTokenResponse response);
    public delegate void PostTweetCallback(bool success);

    public class API
    {
        #region OAuth Token Methods
        // 1. Get Request-Token From Twitter
        // 2. Get PIN from User
        // 3. Get Access-Token from Twitter
        // 4. Use Accss-Token for APIs requriring OAuth 
        // Accss-Token will be always valid until the user revokes the access to your application.

        // Twitter APIs for OAuth process
        private static readonly string RequestTokenURL = "https://api.twitter.com/oauth/request_token";
        private static readonly string AuthorizationURL = "https://api.twitter.com/oauth/authenticate?oauth_token={0}";
        private static readonly string AccessTokenURL = "https://api.twitter.com/oauth/access_token";

        //PINコード発行画面に映るときに使います
        public static IEnumerator GetRequestToken(string consumerKey, string consumerSecret, RequestTokenCallback callback)
        {
            WWW web = WWWRequestToken(consumerKey, consumerSecret);

            yield return web;

            if (!string.IsNullOrEmpty(web.error))
            {
                Debug.Log(string.Format("GetRequestToken - failed. error : {0}", web.error));
                callback(false, null);
            }
            else
            {
                RequestTokenResponse response = new RequestTokenResponse
                {
                    Token = Regex.Match(web.text, @"oauth_token=([^&]+)").Groups[1].Value,
                    TokenSecret = Regex.Match(web.text, @"oauth_token_secret=([^&]+)").Groups[1].Value,
                };

                if (!string.IsNullOrEmpty(response.Token) &&
                    !string.IsNullOrEmpty(response.TokenSecret))
                {
                    callback(true, response);
                }
                else
                {
                    Debug.Log(string.Format("GetRequestToken - failed. response : {0}", web.text));

                    callback(false, null);
                }
            }
        }

        //PINコードによる登録申請の時に使います
        public static void OpenAuthorizationPage(string requestToken)
        {
            Application.OpenURL(string.Format(AuthorizationURL, requestToken));
        }

        public static IEnumerator GetAccessToken(string consumerKey, string consumerSecret, string requestToken, string pin, AccessTokenCallback callback)
        {
            WWW web = WWWAccessToken(consumerKey, consumerSecret, requestToken, pin);

            yield return web;

            if (!string.IsNullOrEmpty(web.error))
            {
                Debug.Log(string.Format("GetAccessToken - failed. error : {0}", web.error));
                callback(false, null);
            }
            else
            {
                AccessTokenResponse response = new AccessTokenResponse
                {
                    Token = Regex.Match(web.text, @"oauth_token=([^&]+)").Groups[1].Value,
                    TokenSecret = Regex.Match(web.text, @"oauth_token_secret=([^&]+)").Groups[1].Value,
                    UserId = Regex.Match(web.text, @"user_id=([^&]+)").Groups[1].Value,
                    ScreenName = Regex.Match(web.text, @"screen_name=([^&]+)").Groups[1].Value
                };

                if (!string.IsNullOrEmpty(response.Token) &&
                    !string.IsNullOrEmpty(response.TokenSecret) &&
                    !string.IsNullOrEmpty(response.UserId) &&
                    !string.IsNullOrEmpty(response.ScreenName))
                {
                    callback(true, response);
                    
                }
                else
                {
                    Debug.Log(string.Format("GetAccessToken - failed. response : {0}", web.text));

                    callback(false, null);
                }
            }
        }

        //上記のGetRequestTokenで使います
        private static WWW WWWRequestToken(string consumerKey, string consumerSecret)
        {
            // Add data to the form to post.
            WWWForm form = new WWWForm();
            form.AddField("oauth_callback", "oob");

            // HTTP header
            Dictionary<string, string> parameters = new Dictionary<string, string>();
            AddDefaultOAuthParams(parameters, consumerKey, consumerSecret);
            parameters.Add("oauth_callback", "oob");

            //var headers = new Hashtable();
            Dictionary<string, string> headers = new Dictionary<string, string>();

            headers["Authorization"] = GetFinalOAuthHeader("POST", RequestTokenURL, parameters);

            return new WWW(RequestTokenURL, form.data, headers);
        }

        //上記のOpenAuthorizationPageで使います
        private static WWW WWWAccessToken(string consumerKey, string consumerSecret, string requestToken, string pin)
        {
            // Need to fill body since Unity doesn't like an empty request body.
            byte[] dummmy = new byte[1];
            dummmy[0] = 0;

            // HTTP header
            //var headers = new Hashtable();
            Dictionary<string, string> headers = new Dictionary<string, string>();

            Dictionary<string, string> parameters = new Dictionary<string, string>();
            AddDefaultOAuthParams(parameters, consumerKey, consumerSecret);
            parameters.Add("oauth_token", requestToken);
            parameters.Add("oauth_verifier", pin);

            headers["Authorization"] = GetFinalOAuthHeader("POST", AccessTokenURL, parameters);

            return new WWW(AccessTokenURL, dummmy, headers);
        }

        private static string GetHeaderWithAccessToken(string httpRequestType, string apiURL, string consumerKey, string consumerSecret, AccessTokenResponse response, Dictionary<string, string> parameters)
        {
            AddDefaultOAuthParams(parameters, consumerKey, consumerSecret);

            parameters.Add("oauth_token", response.Token);
            parameters.Add("oauth_token_secret", response.TokenSecret);

            return GetFinalOAuthHeader(httpRequestType, apiURL, parameters);
        }

        #endregion

        #region Twitter API Methods

        private const string PostTweetURL = "https://api.twitter.com/1.1/statuses/update.json";


        //ツイートを行います
        public static IEnumerator PostTweet(string text, string consumerKey, string consumerSecret, AccessTokenResponse response, PostTweetCallback callback)
        {
            if (string.IsNullOrEmpty(text) || text.Length > 140)
            {
                Debug.Log(string.Format("PostTweet - text[{0}] is empty or too long.", text));

                callback(false);
            }
            else
            {
                Dictionary<string, string> parameters = new Dictionary<string, string>();
                parameters.Add("status", text);

                // Add data to the form to post.
                WWWForm form = new WWWForm();
                form.AddField("status", text);

                // HTTP header
                //var headers = new Hashtable();
                Dictionary<string, string> headers = new Dictionary<string, string>();

                headers["Authorization"] = GetHeaderWithAccessToken("POST", PostTweetURL, consumerKey, consumerSecret, response, parameters);

                WWW web = new WWW(PostTweetURL, form.data, headers);
                yield return web;

                if (!string.IsNullOrEmpty(web.error))
                {
                    Debug.Log(string.Format("PostTweet - failed. {0}\n{1}", web.error, web.text));
                    callback(false);
                }
                else
                {
                    string error = Regex.Match(web.text, @"<error>([^&]+)</error>").Groups[1].Value;

                    if (!string.IsNullOrEmpty(error))
                    {
                        Debug.Log(string.Format("PostTweet - failed. {0}", error));
                        callback(false);
                    }
                    else
                    {
                        callback(true);
                    }
                }
            }
        }

        //メディア付きのツイートURL
        private const string UploadMediaURL = "https://upload.twitter.com/1.1/media/upload.json";


        //ファイルパスから画像ツイート
        public static IEnumerator PostTweetWithMedia(string text, string imagePath, string consumerKey, string consumerSecret, AccessTokenResponse response, PostTweetCallback callback)
        {
            if (string.IsNullOrEmpty(text) || text.Length > 140)
            {
                Debug.Log(string.Format("PostTweet - text[{0}] is empty or too long.", text));

                callback(false);
            }
            else
            {
                FileStream fileStream = new FileStream(imagePath, FileMode.Open, FileAccess.Read);
                BinaryReader bin = new BinaryReader(fileStream);
                byte[] bytes = bin.ReadBytes((int)bin.BaseStream.Length);
                var bs64 = Convert.ToBase64String(bytes);
                bin.Close();

                Dictionary<string, string> mediaParameters = new Dictionary<string, string>
                {
                    { "media_data", bs64 }
                };
                WWWForm mediaForm = new WWWForm();
                mediaForm.AddField("media_data", bs64);
                var mediaHeaders = new Dictionary<string, string>();
                mediaHeaders.Add("Authorization", GetHeaderWithAccessToken("POST", UploadMediaURL, consumerKey, consumerSecret, response, mediaParameters));
                WWW mediaWeb = new WWW(UploadMediaURL, mediaForm.data, mediaHeaders);

                yield return mediaWeb;

                string media_id_string = "";
                if (!string.IsNullOrEmpty(mediaWeb.error))
                {
                    Debug.Log(string.Format("PostMedia - failed. {0}\n{1}", mediaWeb.error, mediaWeb.text));
                    callback(false);
                    yield break;
                }
                else
                {
                    string error = Regex.Match(mediaWeb.text, @"<error>([^&]+)</error>").Groups[1].Value;

                    if (!string.IsNullOrEmpty(error))
                    {
                        Debug.Log(string.Format("PostTweet - failed. {0}", error));
                        callback(false);
                        yield break;
                    }
                    else
                    {
                        var res = JsonUtility.FromJson<mediaResponse>(mediaWeb.text);
                        media_id_string = res.media_id_string;
                    }
                }

                Dictionary<string, string> parameters = new Dictionary<string, string>
                {
                    { "status", text },
                    { "media_ids", media_id_string }
                };
                WWWForm form = new WWWForm();
                form.AddField("status", text);
                form.AddField("media_ids", media_id_string);

                // HTTP header
                var headers = new Dictionary<string, string>();
                headers.Add("Authorization", GetHeaderWithAccessToken("POST", PostTweetURL, consumerKey, consumerSecret, response, parameters));

                WWW web = new WWW(PostTweetURL, form.data, headers);
                yield return web;

                if (!string.IsNullOrEmpty(web.error))
                {
                    Debug.Log(string.Format("PostTweet - failed. {0}\n{1}", web.error, web.text));
                    callback(false);
                }
                else
                {
                    string error = Regex.Match(web.text, @"<error>([^&]+)</error>").Groups[1].Value;

                    if (!string.IsNullOrEmpty(error))
                    {
                        Debug.Log(string.Format("PostTweet - failed. {0}", error));
                        callback(false);
                    }
                    else
                    {
                        callback(true);
                    }
                }
            }
        }

        //Spriteから変換して画像ツイート
        public static IEnumerator PostTweetWithMedia(string _text, Sprite _sprite, string consumerKey, string consumerSecret, AccessTokenResponse response, PostTweetCallback callback)
        {
            if (string.IsNullOrEmpty(_text) || _text.Length > 140)
            {
                Debug.Log(string.Format("PostTweet - text[{0}] is empty or too long.", _text));

                callback(false);
            }
            else
            {
                Texture2D readableTexture = new Texture2D((int)_sprite.rect.width, (int)_sprite.rect.height);
                readableTexture.ReadPixels(_sprite.textureRect, 0, 0);

                var pixels = _sprite.texture.GetPixels((int)_sprite.textureRect.x,
                                         (int)_sprite.textureRect.y,
                                         (int)_sprite.textureRect.width,
                                         (int)_sprite.textureRect.height);

                readableTexture.SetPixels(pixels);
                readableTexture.Apply();

                byte[] bytes = readableTexture.EncodeToPNG();
                var bs64 = Convert.ToBase64String(bytes);

                Dictionary<string, string> mediaParameters = new Dictionary<string, string>
                {
                    { "media_data", bs64 }
                };
                WWWForm mediaForm = new WWWForm();
                mediaForm.AddField("media_data", bs64);
                var mediaHeaders = new Dictionary<string, string>();
                mediaHeaders.Add("Authorization", GetHeaderWithAccessToken("POST", UploadMediaURL, consumerKey, consumerSecret, response, mediaParameters));
                WWW mediaWeb = new WWW(UploadMediaURL, mediaForm.data, mediaHeaders);

                yield return mediaWeb;

                string media_id_string = "";
                if (!string.IsNullOrEmpty(mediaWeb.error))
                {
                    Debug.Log(string.Format("PostMedia - failed. {0}\n{1}", mediaWeb.error, mediaWeb.text));
                    callback(false);
                    yield break;
                }
                else
                {
                    string error = Regex.Match(mediaWeb.text, @"<error>([^&]+)</error>").Groups[1].Value;

                    if (!string.IsNullOrEmpty(error))
                    {
                        Debug.Log(string.Format("PostTweet - failed. {0}", error));
                        callback(false);
                        yield break;
                    }
                    else
                    {
                        var res = JsonUtility.FromJson<mediaResponse>(mediaWeb.text);
                        media_id_string = res.media_id_string;
                    }
                }

                Dictionary<string, string> parameters = new Dictionary<string, string>
                {
                    { "status", _text },
                    { "media_ids", media_id_string }
                };
                WWWForm form = new WWWForm();
                form.AddField("status", _text);
                form.AddField("media_ids", media_id_string);

                // HTTP header
                var headers = new Dictionary<string, string>();
                headers.Add("Authorization", GetHeaderWithAccessToken("POST", PostTweetURL, consumerKey, consumerSecret, response, parameters));

                WWW web = new WWW(PostTweetURL, form.data, headers);
                yield return web;

                if (!string.IsNullOrEmpty(web.error))
                {
                    Debug.Log(string.Format("PostTweet - failed. {0}\n{1}", web.error, web.text));
                    callback(false);
                }
                else
                {
                    string error = Regex.Match(web.text, @"<error>([^&]+)</error>").Groups[1].Value;

                    if (!string.IsNullOrEmpty(error))
                    {
                        Debug.Log(string.Format("PostTweet - failed. {0}", error));
                        callback(false);
                    }
                    else
                    {
                        callback(true);
                    }
                }
            }
        }

        #endregion

        #region OAuth Help Methods
        // The below help methods are modified from "WebRequestBuilder.cs" in Twitterizer(http://www.twitterizer.net/).
        // Here is its license.

        //-----------------------------------------------------------------------
        // <copyright file="WebRequestBuilder.cs" company="Patrick 'Ricky' Smith">
        //  This file is part of the Twitterizer library (http://www.twitterizer.net/)
        // 
        //  Copyright (c) 2010, Patrick "Ricky" Smith (ricky@digitally-born.com)
        //  All rights reserved.
        //  
        //  Redistribution and use in source and binary forms, with or without modification, are 
        //  permitted provided that the following conditions are met:
        // 
        //  - Redistributions of source code must retain the above copyright notice, this list 
        //    of conditions and the following disclaimer.
        //  - Redistributions in binary form must reproduce the above copyright notice, this list 
        //    of conditions and the following disclaimer in the documentation and/or other 
        //    materials provided with the distribution.
        //  - Neither the name of the Twitterizer nor the names of its contributors may be 
        //    used to endorse or promote products derived from this software without specific 
        //    prior written permission.
        // 
        //  THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND 
        //  ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED 
        //  WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. 
        //  IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, 
        //  INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT 
        //  NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR 
        //  PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, 
        //  WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) 
        //  ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE 
        //  POSSIBILITY OF SUCH DAMAGE.
        // </copyright>
        // <author>Ricky Smith</author>
        // <summary>Provides the means of preparing and executing Anonymous and OAuth signed web requests.</summary>
        //-----------------------------------------------------------------------

        private static readonly string[] OAuthParametersToIncludeInHeader = new[]
                                                          {
                                                              "oauth_version",
                                                              "oauth_nonce",
                                                              "oauth_timestamp",
                                                              "oauth_signature_method",
                                                              "oauth_consumer_key",
                                                              "oauth_token",
                                                              "oauth_verifier"
                                                              // Leave signature omitted from the list, it is added manually
                                                              // "oauth_signature",
                                                          };

        private static readonly string[] SecretParameters = new[]
                                                                {
                                                                    "oauth_consumer_secret",
                                                                    "oauth_token_secret",
                                                                    "oauth_signature"
                                                                };

        private static void AddDefaultOAuthParams(Dictionary<string, string> parameters, string consumerKey, string consumerSecret)
        {
            parameters.Add("oauth_version", "1.0");
            parameters.Add("oauth_nonce", GenerateNonce());
            parameters.Add("oauth_timestamp", GenerateTimeStamp());
            parameters.Add("oauth_signature_method", "HMAC-SHA1");
            parameters.Add("oauth_consumer_key", consumerKey);
            parameters.Add("oauth_consumer_secret", consumerSecret);
        }

        private static string GetFinalOAuthHeader(string HTTPRequestType, string URL, Dictionary<string, string> parameters)
        {
            // Add the signature to the oauth parameters
            string signature = GenerateSignature(HTTPRequestType, URL, parameters);

            parameters.Add("oauth_signature", signature);

            StringBuilder authHeaderBuilder = new StringBuilder();
            authHeaderBuilder.AppendFormat("OAuth realm=\"{0}\"", "Twitter API");

            var sortedParameters = from p in parameters
                                   where OAuthParametersToIncludeInHeader.Contains(p.Key)
                                   orderby p.Key, UrlEncode(p.Value)
                                   select p;

            foreach (var item in sortedParameters)
            {
                authHeaderBuilder.AppendFormat(",{0}=\"{1}\"", UrlEncode(item.Key), UrlEncode(item.Value));
            }

            authHeaderBuilder.AppendFormat(",oauth_signature=\"{0}\"", UrlEncode(parameters["oauth_signature"]));

            return authHeaderBuilder.ToString();
        }

        private static string GenerateSignature(string httpMethod, string url, Dictionary<string, string> parameters)
        {
            var nonSecretParameters = (from p in parameters
                                       where !SecretParameters.Contains(p.Key)
                                       select p);

            // Create the base string. This is the string that will be hashed for the signature.
            string signatureBaseString = string.Format(CultureInfo.InvariantCulture,
                                                       "{0}&{1}&{2}",
                                                       httpMethod,
                                                       UrlEncode(NormalizeUrl(new Uri(url))),
                                                       UrlEncode(nonSecretParameters));

            // Create our hash key (you might say this is a password)
            string key = string.Format(CultureInfo.InvariantCulture,
                                       "{0}&{1}",
                                       UrlEncode(parameters["oauth_consumer_secret"]),
                                       parameters.ContainsKey("oauth_token_secret") ? UrlEncode(parameters["oauth_token_secret"]) : string.Empty);


            // Generate the hash
            HMACSHA1 hmacsha1 = new HMACSHA1(Encoding.ASCII.GetBytes(key));
            byte[] signatureBytes = hmacsha1.ComputeHash(Encoding.ASCII.GetBytes(signatureBaseString));
            return Convert.ToBase64String(signatureBytes);
        }

        private static string GenerateTimeStamp()
        {
            // Default implementation of UNIX time of the current UTC time
            TimeSpan ts = DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, 0);
            return Convert.ToInt64(ts.TotalSeconds, CultureInfo.CurrentCulture).ToString(CultureInfo.CurrentCulture);
        }

        private static string GenerateNonce()
        {
            // Just a simple implementation of a random number between 123400 and 9999999
            return new System.Random().Next(123400, int.MaxValue).ToString("X", CultureInfo.InvariantCulture);
        }

        private static string NormalizeUrl(Uri url)
        {
            string normalizedUrl = string.Format(CultureInfo.InvariantCulture, "{0}://{1}", url.Scheme, url.Host);
            if (!((url.Scheme == "http" && url.Port == 80) || (url.Scheme == "https" && url.Port == 443)))
            {
                normalizedUrl += ":" + url.Port;
            }

            normalizedUrl += url.AbsolutePath;
            return normalizedUrl;
        }

        private static string UrlEncode(string value)
        {
            //一時変数を用意してコピーし、valueをからにした後どんどん+=していく感じです。

            string tmp = string.Copy(value);
            value = "";
            if (!(tmp.Length >= 10000))
            {
                // Debug.Log(Uri.EscapeDataString(tmp).Length);
                value += Uri.EscapeDataString(tmp);
            }
            else
            {
                int count = 0;
                for (int i = 0; i < tmp.Length; i += 10000)
                {
                    if (i + 10000 >= tmp.Length)
                    {
                        value += Uri.EscapeDataString(tmp.Substring(i));
                        Debug.Log(count + "times");
                        break;
                    }
                    value += Uri.EscapeDataString(tmp.Substring(i, 10000));
                    count++;
                }
            }

            return value;
        }

        private static string UrlEncode(IEnumerable<KeyValuePair<string, string>> parameters)
        {
            StringBuilder parameterString = new StringBuilder();

            var paramsSorted = from p in parameters
                               orderby p.Key, p.Value
                               select p;

            foreach (var item in paramsSorted)
            {
                if (parameterString.Length > 0)
                {
                    parameterString.Append("&");
                }

                parameterString.Append(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "{0}={1}",
                        UrlEncode(item.Key),
                        UrlEncode(item.Value)));
            }

            return UrlEncode(parameterString.ToString());
        }

        #endregion
    }
}

// １回目のレスポンスJSONをオブジェクトに変換するためのクラス
class mediaResponse
{
    public long media_id = 0;
    public string media_id_string = "";
}
