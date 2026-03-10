using UnityEngine;
using TMPro;
using Firebase.Firestore;

public class DashboardManager : MonoBehaviour
{
    public TextMeshProUGUI totalUserCount;
    public TextMeshProUGUI totalStudentCount;
    public TextMeshProUGUI totalTeacherCount;

    private FirebaseFirestore db;
    private ListenerRegistration userListener;

    void Start()
    {
        db = FirebaseFirestore.DefaultInstance;
        ListenToUserChanges();
    }

    void ListenToUserChanges()
    {
        CollectionReference usersRef = db.Collection("users");

        userListener = usersRef.Listen(snapshot =>
        {
            int totalUsers = 0;
            int studentCount = 0;
            int teacherCount = 0;

            foreach (DocumentSnapshot doc in snapshot.Documents)
{
    if (!doc.TryGetValue("is_verified", out bool isVerified) || !isVerified) continue;

    if (doc.TryGetValue("role", out string role))
                {
                    if (role == "student")
                    {
                        studentCount++;
                        totalUsers++;
                    }
                    else if (role == "teacher")
                    {
                        teacherCount++;
                        totalUsers++;
                    }
                }
            }

            totalUserCount.text = totalUsers.ToString();
            totalStudentCount.text = studentCount.ToString();
            totalTeacherCount.text = teacherCount.ToString();
        });
    }

    void OnDestroy()
    {
        if (userListener != null)
        {
            userListener.Stop();
        }
    }
}