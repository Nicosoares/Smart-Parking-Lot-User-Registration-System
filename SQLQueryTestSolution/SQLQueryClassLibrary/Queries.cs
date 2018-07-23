using Microsoft.ProjectOxford.Face;
using Microsoft.ProjectOxford.Face.Contract;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace SQLQueryClassLibrary
{
    public class Queries
    {
        private SqlConnection connection = new SqlConnection("Server=tcp:sparking.database.windows.net,1433;Initial Catalog=ParkingDatabse;Persist Security Info=False;User ID=parkingAdmin;Password=p@ssw0rd;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;");
        private string saveUserQuery = "INSERT INTO UserData (UserFName1, UserFName2, UserLName1, AuthorizationStatus) VALUES (@firstname, @middlename, @lastname, @authstatus) SELECT SCOPE_IDENTITY()";
        //private string getSameUserQuery = "SELECT SCOPE_IDENTITY() FROM UserData WHERE UserFName1 = @firstname AND UserFName2 = @middlename AND UserLName1 = @lastname";
        private string getLatestLPersonGroupQuery = "SELECT LargePersonGroupID FROM LargePersonGroup WHERE CurrentCapacity < 1000";
        private string saveNewLPersonGroupQuery = "INSERT INTO LargePersonGroup (CurrentCapacity) VALUES (0) SELECT SCOPE_IDENTITY()";
        private string updateUserInfoQuery = "UPDATE UserData SET LargePersonGroupID = @lpersonID, UserPersonID = @personID WHERE UserID = @userID";
        private string updateLPersonGroupCountQuery = "UPDATE LargePersonGroup SET CurrentCapacity = CurrentCapacity + 1 WHERE LargePersonGroupID = @lPersonGroupID";
        private string getUserPersonAndGroupIDQuery = "SELECT UserPersonID, LargePersonGroupID FROM UserData WHERE UserID = @userID";

        public Queries() { }

        public string InsertNewUser(string firstName, string middleName, string lastName)
        {
            string userID = null;
            using (SqlCommand saveUser = new SqlCommand(saveUserQuery))
            {
                saveUser.Connection = connection;
                saveUser.Parameters.Add("@firstname", SqlDbType.VarChar, 25).Value = firstName;
                saveUser.Parameters.Add("@middlename", SqlDbType.VarChar, 25).Value = middleName;
                saveUser.Parameters.Add("@lastname", SqlDbType.VarChar, 25).Value = lastName;
                saveUser.Parameters.Add("@authstatus", SqlDbType.Bit).Value = 1;

                connection.Open();
                using (SqlDataReader reader = saveUser.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var result = reader.GetValue(0);
                        userID = result.ToString();
                    }
                }
                connection.Close();
            }
            return userID;
        }

        /*public string LatestUserID(string firstName, string middleName, string lastName)
        {
            string userID = null;

            using (SqlCommand getSameuser = new SqlCommand(getSameUserQuery))
            {
                getSameuser.Connection = connection;
                getSameuser.Parameters.Add("@firstname", SqlDbType.VarChar, 25).Value = firstName;
                getSameuser.Parameters.Add("@middlename", SqlDbType.VarChar, 25).Value = middleName;
                getSameuser.Parameters.Add("@lastname", SqlDbType.VarChar, 25).Value = lastName;

                connection.Open();
                using (SqlDataReader reader = getSameuser.ExecuteReader())
                {
                    userID = reader.GetString(1);
                }
                connection.Close();
            }

            return userID;
        }*/

        public async Task<string>  AvailableLPersonGroup()
        {
            string LPersongGroupID = null;
            using (SqlCommand getLatestLPersonGroup = new SqlCommand(getLatestLPersonGroupQuery))
            {
                getLatestLPersonGroup.Connection = connection;
                connection.Open();
                using (SqlDataReader reader = getLatestLPersonGroup.ExecuteReader())
                {
                    if(reader.Read())
                    {
                        var result = reader.GetValue(0);
                        LPersongGroupID = result.ToString();
                    }
                }
                connection.Close();
            }
            if (LPersongGroupID != null)
                return LPersongGroupID;


            using (SqlCommand saveNewLPersonGroup = new SqlCommand(saveNewLPersonGroupQuery))
            {
                saveNewLPersonGroup.Connection = connection;
                connection.Open();
                using (SqlDataReader reader = saveNewLPersonGroup.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        var result = reader.GetValue(0);
                        LPersongGroupID = result.ToString();
                    }
                }
                connection.Close();
            }

            IFaceServiceClient faceServiceClient = new FaceServiceClient("ae3512e532c545ba9e821202a1bbd350", "https://eastus.api.cognitive.microsoft.com/face/v1.0");
            await faceServiceClient.CreateLargePersonGroupAsync(LPersongGroupID, LPersongGroupID, LPersongGroupID).ConfigureAwait(false);
            return LPersongGroupID;
        }

        public async Task<CreatePersonResult> CreatePersonAsync(string LPersonGroupID, string Name, string ID)
        {
            IFaceServiceClient faceServiceClient = new FaceServiceClient("ae3512e532c545ba9e821202a1bbd350", "https://eastus.api.cognitive.microsoft.com/face/v1.0");
            return await faceServiceClient.CreatePersonInLargePersonGroupAsync(LPersonGroupID, Name, ID).ConfigureAwait(false);
        }

        public async Task<Guid> AddPersonToGroup(string LPersonGroupID, string Name, string ID)
        {
            using (SqlCommand updateLPersonGroupCount = new SqlCommand(updateLPersonGroupCountQuery))
            {
                updateLPersonGroupCount.Connection = connection;
                updateLPersonGroupCount.Parameters.Add("@lPersonGroupID", SqlDbType.Int).Value = LPersonGroupID;
                connection.Open();
                updateLPersonGroupCount.ExecuteNonQuery();
                connection.Close();
            }
            var person = await CreatePersonAsync(LPersonGroupID, Name, ID);
            return person.PersonId;
        }

        public int UpdateUserInfo(string userID, string lpersonGroupID, Guid personID)
        {
            int row;
            using (SqlCommand updateUserInfo = new SqlCommand(updateUserInfoQuery))
            {
                updateUserInfo.Connection = connection;
                updateUserInfo.Parameters.Add("@userID", SqlDbType.Int).Value = userID;
                updateUserInfo.Parameters.Add("@lpersonID", SqlDbType.Int).Value = lpersonGroupID;
                updateUserInfo.Parameters.Add("@personID", SqlDbType.UniqueIdentifier).Value = personID;

                connection.Open();
                row = updateUserInfo.ExecuteNonQuery();
                connection.Close();
            }
            return row;
        }

        public async void AddFaceToPerson(int UserID, StorageFile file)
        {
            Guid userGuid = new Guid();
            string userPersonGroupID = null;
            using (SqlCommand getUserPersonAndGroupID = new SqlCommand(getUserPersonAndGroupIDQuery))
            {
                getUserPersonAndGroupID.Connection = connection;
                getUserPersonAndGroupID.Parameters.Add("@userID", SqlDbType.Int).Value = UserID;
                connection.Open();
                using (SqlDataReader reader = getUserPersonAndGroupID.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        userGuid = reader.GetGuid(0);
                        var result = reader.GetValue(1);
                        userPersonGroupID = result.ToString();
                    }
                }
                connection.Close();
            }

            IFaceServiceClient faceServiceClient = new FaceServiceClient("ae3512e532c545ba9e821202a1bbd350", "https://eastus.api.cognitive.microsoft.com/face/v1.0");
            using (Stream s = await file.OpenStreamForReadAsync())
            {
                await faceServiceClient.AddPersonFaceInLargePersonGroupAsync(userPersonGroupID, userGuid, s, null, null);
            }                
            await faceServiceClient.TrainLargePersonGroupAsync(userPersonGroupID);

            var trainingStatus = await faceServiceClient.GetLargePersonGroupTrainingStatusAsync(userPersonGroupID);
            while (trainingStatus.Status == Status.Running)
            {
                trainingStatus = await faceServiceClient.GetLargePersonGroupTrainingStatusAsync(userPersonGroupID);
            }
        }
    }
}
