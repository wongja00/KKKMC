using UnityEngine;
using Firebase;
using Firebase.Auth;
using Firebase.Database;
using Firebase.Extensions;
using System.Threading.Tasks;
public class FirebaseInit : MonoBehaviour
{
    public static FirebaseAuth Auth;
    public static DatabaseReference DB;

    public static FirebaseApp AppInstance;

    async void Awake()
    {
        var dependencyStatus = await FirebaseApp.CheckDependenciesAsync();
        if(dependencyStatus == Firebase.DependencyStatus.Available)
        {
            Debug.Log("파베 초기화!");
            
            AppInstance = FirebaseApp.DefaultInstance;
            System.Uri dbUrl = new System.Uri("https://kkkmc-matchmaking-default-rtdb.firebaseio.com/");
            AppInstance.Options.DatabaseUrl = dbUrl;
            
            Auth = FirebaseAuth.GetAuth(AppInstance);
            await SignInAnonymously();
            DB = FirebaseDatabase.GetInstance(AppInstance, dbUrl.ToString()).RootReference;

            if(DB == null)
            {
                Debug.Log("DB null");
            }
        }
        else
        {
            Debug.LogError("파베 오류: " + dependencyStatus);
        }
    }

    private async Task SignInAnonymously()
    {
        await Auth.SignInAnonymouslyAsync();
        Debug.Log("로그인 성공");
    }
}
