using UnityEngine;
using Google;
using System.Threading.Tasks;
using TMPro;
using Firebase.Auth;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.Networking;
using Firebase;
using Firebase.Database;
using MultiTechStudio.Cooking2D;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class GoogleLogin : MonoBehaviour
{
    public string GoogleAPI = "1085020510151-k9ocvosabf6ck9aqmbcomnb0rh1t7162.apps.googleusercontent.com";

    private GoogleSignInConfiguration configuration;
    FirebaseAuth auth;
    FirebaseUser user;

    public TextMeshProUGUI Username, UserEmail;
    public Image UserProfilePic;

    private string imageUrl;
    private bool isGooglesignInInitialized = false;
    private DatabaseReference db;

    public GameObject loginpanel;
    public static GoogleLogin instance;
    public static bool FirebaseReady = false;

    private void Awake()
    {
        instance = this;
    }
    IEnumerator Start()
    {
        if (PlayerPrefs.GetInt("IsLoggedIn", 0) == 1)
        {
            loginpanel.SetActive(false);
        }

        var check = FirebaseApp.CheckAndFixDependenciesAsync();
        yield return new WaitUntil(() => check.IsCompleted);

        if (check.Result == DependencyStatus.Available)
        {
            InitFirebase();
            Debug.Log("Firebase Ready!");

            user = FirebaseAuth.DefaultInstance.CurrentUser;

            if (user != null)
            {
                loginpanel.SetActive(false);

                PlayerPrefs.SetInt("IsLoggedIn", 1);
                PlayerPrefs.Save();

                Username.text = user.DisplayName;
                UserEmail.text = user.Email;

                StartCoroutine(LoadImage(CheckImageUrl(user.PhotoUrl.ToString())));
            }
            else
            {
                if (PlayerPrefs.GetInt("IsLoggedIn", 0) == 0)
                    loginpanel.SetActive(true);
            }
        }
    }

    void InitFirebase()
    {
        FirebaseReady = true;

        FirebaseApp app = FirebaseApp.DefaultInstance;

        app.Options.DatabaseUrl = new System.Uri(
            "https://cookinggame-92a66-default-rtdb.firebaseio.com/"
        );

        auth = FirebaseAuth.DefaultInstance;
        db = FirebaseDatabase.GetInstance(app).RootReference;

        Debug.Log("Firebase Initialized Correctly!");
    }




    /* public void Login()
     {

         if (!isGooglesignInInitialized)
         {
             GoogleSignIn.Configuration = new GoogleSignInConfiguration
             {
                 RequestIdToken = true,
                 WebClientId = GoogleAPI,
                 RequestEmail = true
             };

             isGooglesignInInitialized = true;
         }

         *//*GoogleSignIn.Configuration = new GoogleSignInConfiguration
         {
             RequestIdToken = true,
             WebClientId = GoogleAPI
         };

         GoogleSignIn.Configuration.RequestEmail = true;*//*

         Task<GoogleSignInUser> signIn = GoogleSignIn.DefaultInstance.SignIn();

         TaskCompletionSource<FirebaseUser> signInCompleted = new TaskCompletionSource<FirebaseUser>();

         signIn.ContinueWith(task =>
         {
             if (task.IsCanceled)
             {
                 signInCompleted.SetCanceled();
                 Debug.Log("Cancelled");
             }
             else if (task.IsFaulted)
             {
                 signInCompleted.SetException(task.Exception);
                 Debug.Log("Faulted " + task.Exception);
             }
             else
             {
                 Credential credential = GoogleAuthProvider.GetCredential(
                     ((Task<GoogleSignInUser>)task).Result.IdToken,
                     null);

                 auth.SignInWithCredentialAsync(credential).ContinueWith(authTask =>
                 {
                     if (authTask.IsCanceled)
                     {
                         signInCompleted.SetCanceled();
                     }
                     else if (authTask.IsFaulted)
                     {
                         signInCompleted.SetException(authTask.Exception);
                     }
                     else
                     {
                         auth = FirebaseAuth.DefaultInstance;
                         Debug.Log("Success");

                         user = auth.CurrentUser;
                         loginpanel.SetActive(false);


                         Username.text = user.DisplayName;
                         UserEmail.text = user.Email;
                         StartCoroutine(LoadImage(CheckImageUrl(user.PhotoUrl.ToString())));
                         //SavePlayerDataToFirebase();

                         LoadPlayerDataFromFirebase();
                     }
                 });
             }
         });
     }*/
    public void Login()
    {
        if (!FirebaseReady)
        {
            Debug.LogError("Firebase not ready!");
            return;
        }

        if (!isGooglesignInInitialized)
        {
            GoogleSignIn.Configuration = new GoogleSignInConfiguration
            {
                RequestIdToken = true,
                WebClientId = GoogleAPI,
                RequestEmail = true,
                UseGameSignIn = false,
                ForceTokenRefresh = true,    // avoids silent login

            };

            isGooglesignInInitialized = true;
        }


        GoogleSignIn.DefaultInstance.SignIn().ContinueWith(task =>
        {
            if (task.IsFaulted || task.IsCanceled)
            {
                Debug.LogError("Google Sign-In failed.");
                return;
            }

            var googleUser = task.Result;
            Credential credential = GoogleAuthProvider.GetCredential(googleUser.IdToken, null);

            auth.SignInWithCredentialAsync(credential).ContinueWith(authTask =>
            {
                if (authTask.IsFaulted || authTask.IsCanceled)
                {
                    Debug.LogError("Firebase Auth failed.");
                    return;
                }

                user = auth.CurrentUser;

                loginpanel.SetActive(false);
                PlayerPrefs.SetInt("IsLoggedIn", 1);
                PlayerPrefs.Save();
                Username.text = user.DisplayName;
                UserEmail.text = user.Email;

                StartCoroutine(LoadImage(CheckImageUrl(user.PhotoUrl.ToString())));

                LoadPlayerDataFromFirebase();


            });
        });
    }

    private string CheckImageUrl(string url)
    {
        if (!string.IsNullOrEmpty(url))
        {
            return url;
        }
        return imageUrl;
    }

    IEnumerator LoadImage(string imageUri)
    {
        UnityWebRequest www = UnityWebRequestTexture.GetTexture(imageUri);
        yield return www.SendWebRequest();

        if (www.result == UnityWebRequest.Result.Success)
        {
            Texture2D texture = DownloadHandlerTexture.GetContent(www);

            Debug.Log("Image loaded successfully!");

            UserProfilePic.sprite = Sprite.Create(texture,
                new Rect(0, 0, texture.width, texture.height),
                new Vector2(0, 0));
        }
        else
        {
            Debug.Log("Failed to load image.");
        }
    }

    /* public void SavePlayerDataToFirebase()
     {
         if (user == null) return;

         PlayerData data = new PlayerData();

         data.coins = UiManager.TotalCoins;
         data.diamond = UiManager.TotalDiamond;
         data.key = UiManager.TotalKey;
         data.energy = UiManager.TotalEnergy;

         data.level = LevelManager.LevelDisplay;
         foreach (var b in BoosterManager.instance.allBoosters)
             data.boosters[b.booster.ToString()] = b.count;

         db.Child("users").Child(user.UserId)
             .SetRawJsonValueAsync(JsonUtility.ToJson(data));
     }*/
    public void SavePlayerDataToFirebase()
    {
        if (user == null || db == null)
        {
            Debug.LogError("Cannot save: user or db is null");
            return;
        }

        Dictionary<string, object> data = new Dictionary<string, object>();

        data["Coins"] = UiManager.TotalCoins;
        data["Diamond"] = UiManager.TotalDiamond;
        data["Key"] = UiManager.TotalKey;
        data["Energy"] = UiManager.TotalEnergy;
        data["Level"] = LevelManager.LevelDisplay;

        data["UpgradeData"] = GetAllUpgradeData();

        db.Child("users").Child(user.UserId).UpdateChildrenAsync(data);
    }



    /*   public void LoadPlayerDataFromFirebase()
       {
           db.Child("users").Child(user.UserId).GetValueAsync().ContinueWith(task =>
           {
               if (task.IsCompleted && task.Result.Exists)
               {
                   string json = task.Result.GetRawJsonValue();
                   PlayerData data = JsonUtility.FromJson<PlayerData>(json);

                   PlayerPrefs.SetInt("Coins", data.coins);
                   PlayerPrefs.SetInt("Diamond", data.diamond);
                   PlayerPrefs.SetInt("Key", data.key);
                   PlayerPrefs.SetInt("Energy", data.energy);

                   int loadedLevel = Mathf.Clamp(data.level, 1, LevelManager.Instance.allLevelConfigs.Length); 
                   PlayerPrefs.SetInt(UiManager.LvlDisplay, loadedLevel); 
                   PlayerPrefs.SetInt(UiManager.LvlName, loadedLevel - 1); 
                   LevelManager.LevelIndex = loadedLevel - 1;
                   LevelManager.LevelDisplay = loadedLevel;


                   foreach (var b in BoosterManager.instance.allBoosters)
                   {
                       if (data.boosters.ContainsKey(b.booster.ToString()))
                       {
                           b.count = data.boosters[b.booster.ToString()];
                           PlayerPrefs.SetInt(b.booster + "Count", b.count);
                       }
                   }

                   Debug.Log("Firebase Loaded");

               }

           });
       }*/

    public void LoadPlayerDataFromFirebase()
    {
        StartCoroutine(LoadPlayerDataRoutine());
    }

    IEnumerator LoadPlayerDataRoutine()
    {
        if (db == null)
        {
            Debug.LogError("DB is null � Firebase not ready!");
            yield break;
        }

        var dbTask = db.Child("users").Child(user.UserId).GetValueAsync();
        yield return new WaitUntil(() => dbTask.IsCompleted);

        DataSnapshot snapshot = dbTask.Result;

        if (snapshot.Exists)
        {
            Debug.Log("Existing user found � loading...");

            PlayerPrefs.SetInt("Coins", ToInt(snapshot, "Coins"));
            PlayerPrefs.SetInt("Diamond", ToInt(snapshot, "Diamond"));
            PlayerPrefs.SetInt("Key", ToInt(snapshot, "Key"));
            PlayerPrefs.SetInt("Energy", ToInt(snapshot, "Energy"));

            int level = ToInt(snapshot, "Level");
            level = Mathf.Clamp(level, 1, LevelManager.Instance.allLevelConfigs.Length);

            PlayerPrefs.SetInt(UiManager.LvlDisplay, level);
            PlayerPrefs.SetInt(UiManager.LvlName, level - 1);

            LevelManager.LevelDisplay = level;
            LevelManager.LevelIndex = level - 1;

            if (snapshot.Child("boosters").Exists)
            {
                foreach (var booster in BoosterManager.instance.allBoosters)
                {
                    var key = booster.booster.ToString();

                    if (snapshot.Child("boosters").HasChild(key))
                    {
                        int count = int.Parse(snapshot.Child("boosters").Child(key).Value.ToString());
                        booster.count = count;
                        PlayerPrefs.SetInt(key + "Count", count);
                    }
                }
            }

            if (snapshot.Child("UpgradeData").Exists)
            {
                foreach (var child in snapshot.Child("UpgradeData").Children)
                {
                    PlayerPrefs.SetInt(child.Key, int.Parse(child.Value.ToString()));
                }
            }

        }
        else
        {
            Debug.Log("New User � creating default data");

            PlayerPrefs.DeleteAll();
            PlayerPrefs.Save();

            SavePlayerDataToFirebase();
        }

        PlayerPrefs.Save();

        // Wait 2 frames to ensure all systems read PlayerPrefs
        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();

        SceneManager.LoadScene("Demo Scene");
    }

    private int ToInt(DataSnapshot snap, string key)
    {
        return snap.HasChild(key) ? int.Parse(snap.Child(key).Value.ToString()) : 0;
    }


    private Dictionary<string, object> GetAllUpgradeData()
    {
        Dictionary<string, object> upgradeData = new Dictionary<string, object>();

        foreach (var u in UpgradeManager.Instance.level1UpgradeData)
        {
            string key = UpgradeManager.Instance.levelName
                         + u.objectType + u.isMaking + "Level";
            upgradeData[key] = PlayerPrefs.GetInt(key, 0);

            string unlockKey = UpgradeManager.Instance.levelName
                               + u.objectType + "Unlocked";
            upgradeData[unlockKey] = PlayerPrefs.GetInt(unlockKey, 0);
        }

        foreach (var u in UpgradeManager.Instance.level2UpgradeData)
        {
            string key = UpgradeManager.Instance.levelName
                         + u.objectType + u.isMaking + "Level";
            upgradeData[key] = PlayerPrefs.GetInt(key, 0);

            string unlockKey = UpgradeManager.Instance.levelName
                               + u.objectType + "Unlocked";
            upgradeData[unlockKey] = PlayerPrefs.GetInt(unlockKey, 0);
        }

        foreach (var u in UpgradeManager.Instance.level3UpgradeData)
        {
            string key = UpgradeManager.Instance.levelName
                         + u.objectType + u.isMaking + "Level";
            upgradeData[key] = PlayerPrefs.GetInt(key, 0);

            string unlockKey = UpgradeManager.Instance.levelName
                               + u.objectType + "Unlocked";
            upgradeData[unlockKey] = PlayerPrefs.GetInt(unlockKey, 0);
        }

        return upgradeData;
    }

    public void LogOut()
    {
        AudioManager.Instance.MakeVibrate(VibrationType.Low);
        GoogleSignIn.DefaultInstance.SignOut();
        auth.SignOut();
        FirebaseAuth.DefaultInstance.SignOut();
        PlayerPrefs.SetInt("IsLoggedIn", 0);  
        PlayerPrefs.Save();

        loginpanel.SetActive(true);

        Debug.Log("Logout complete. All PlayerPrefs cleared.");
    }

}
