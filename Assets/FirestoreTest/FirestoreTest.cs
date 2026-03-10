using UnityEngine;
using Firebase;
using Firebase.Firestore;
using Firebase.Extensions;
using System.Collections.Generic;

public class FirestoreTest : MonoBehaviour
{
    FirebaseFirestore db;

    void Start()
    {
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
        {
            if (task.Result == DependencyStatus.Available)
            {
                Debug.Log("Firebase ready!");

                db = FirebaseFirestore.DefaultInstance;

                SaveTestData();
            }
            else
            {
                Debug.LogError("Firebase NOT ready: " + task.Result);
            }
        });
    }

    void SaveTestData()
    {
        DocumentReference docRef = db.Collection("testCollection").Document("testDocument");

        Dictionary<string, object> data = new Dictionary<string, object>()
        {
            { "message", "Hello from Unity!" },
            { "score", 100 },
            { "time", System.DateTime.Now.ToString() }
        };

        docRef.SetAsync(data).ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted)
                Debug.Log("Data saved successfully!");
            else
                Debug.LogError("Error saving data");
        });
    }
}