using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Services;
using MySql.Data;
using MySql.Data.MySqlClient;
using System.Data;
using System.Net;
using System.Net.Mail;

namespace ProjectTemplate
{
	[WebService(Namespace = "http://tempuri.org/")]
	[WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
	[System.ComponentModel.ToolboxItem(false)]
	[System.Web.Script.Services.ScriptService]

	public class ProjectServices : System.Web.Services.WebService
	{
		////////////////////////////////////////////////////////////////////////
		///replace the values of these variables with your database credentials
		////////////////////////////////////////////////////////////////////////
		private string dbID = "abracadevs";
		private string dbPass = "!!Abracadevs";
		private string dbName = "abracadevs";
		////////////////////////////////////////////////////////////////////////
		
		////////////////////////////////////////////////////////////////////////
		///call this method anywhere that you need the connection string!
		////////////////////////////////////////////////////////////////////////
		private string getConString() {
			return "SERVER=107.180.1.16; PORT=3306; DATABASE=" + dbName+"; UID=" + dbID + "; PASSWORD=" + dbPass;
		}
		////////////////////////////////////////////////////////////////////////



		/////////////////////////////////////////////////////////////////////////
		//don't forget to include this decoration above each method that you want
		//to be exposed as a web service!
		[WebMethod(EnableSession = true)]
		/////////////////////////////////////////////////////////////////////////
		public string TestConnection()
		{
			try
			{
				string testQuery = "select * from users";

				////////////////////////////////////////////////////////////////////////
				///here's an example of using the getConString method!
				////////////////////////////////////////////////////////////////////////
				MySqlConnection con = new MySqlConnection(getConString());
				////////////////////////////////////////////////////////////////////////

				MySqlCommand cmd = new MySqlCommand(testQuery, con);
				MySqlDataAdapter adapter = new MySqlDataAdapter(cmd);
				DataTable table = new DataTable();
				adapter.Fill(table);
				return "Success!";
			}
			catch (Exception e)
			{
				return "Something went wrong, please check your credentials and db name and try again.  Error: "+e.Message;
			}
		}

        [WebMethod]

        public string AddUser(string first, string last, string email, string pass, string pw, string licenseplate)
        {

            ///webmethod to a newuser to the database
            ///

            string sqlConnectString = System.Configuration.ConfigurationManager.ConnectionStrings["myDB"].ConnectionString;

            string sqlSelect = "INSERT INTO `abracadevs`.`users` (`first_name`, `last_name`, `email`, `permit`, `status`, `admin`, `license_plate`, `password`)" +
                "VALUES (@fnameValue, @lnameValue, @emailValue, @permitValue, 'pending', '0', @licenseValue, @passwordValue);";

            MySqlConnection sqlConnection = new MySqlConnection(sqlConnectString);
            MySqlCommand sqlCommand = new MySqlCommand(sqlSelect, sqlConnection);


            sqlCommand.Parameters.AddWithValue("@fnameValue", HttpUtility.UrlDecode(first));
            sqlCommand.Parameters.AddWithValue("@lnameValue", HttpUtility.UrlDecode(last));
            sqlCommand.Parameters.AddWithValue("@emailValue", HttpUtility.UrlDecode(email));
            sqlCommand.Parameters.AddWithValue("@permitValue", HttpUtility.UrlDecode(pass));
            sqlCommand.Parameters.AddWithValue("@licenseValue", HttpUtility.UrlDecode(licenseplate));
            sqlCommand.Parameters.AddWithValue("@passwordValue", HttpUtility.UrlDecode(pw));

            sqlConnection.Open();
            try
            {
                sqlCommand.ExecuteNonQuery();
                return "Success!";

            }

            catch (Exception e)
            {
                var str = e.ToString();
                // Data base is setup to require a unique email and license plate. If a duplicate of either is put in the server will return an error. The follow code will
                // search the error returned from the server and return appropriate feedback.

                if (str.Contains("email_UNIQUE"))
                {
                    return "email";
                }

                if (str.Contains("license_plate_UNIQUE"))
                {
                    return "plate";
                }
                else
                {
                    return str;
                }
            }
            sqlConnection.Close();

        }

        [WebMethod]
        public Account[] GetAccountRequests()
        {//LOGIC: get all account requests and return them!

            DataTable sqlDt = new DataTable("users");


            string sqlConnectString = System.Configuration.ConfigurationManager.ConnectionStrings["myDB"].ConnectionString;
            //requests just have active set to 0
            string sqlSelect = "select user_id, first_name, last_name, email from users where status= 'pending' order by last_name";

            MySqlConnection sqlConnection = new MySqlConnection(sqlConnectString);
            MySqlCommand sqlCommand = new MySqlCommand(sqlSelect, sqlConnection);

            MySqlDataAdapter sqlDa = new MySqlDataAdapter(sqlCommand);
            sqlDa.Fill(sqlDt);

            List<Account> users = new List<Account>();
            for (int i = 0; i < sqlDt.Rows.Count; i++)
            {
                users.Add(new Account
                {
                    user_id = Convert.ToInt32(sqlDt.Rows[i]["user_id"]),
                    first_name = sqlDt.Rows[i]["first_name"].ToString(),
                    last_name = sqlDt.Rows[i]["last_name"].ToString(),
                    email = sqlDt.Rows[i]["email"].ToString()
                });
            }
            //convert the list of accounts to an array and return!
            return users.ToArray();

        }
        [WebMethod]
        public void ActivateAccount(int user_id, string email )
        {
            string sqlConnectString = System.Configuration.ConfigurationManager.ConnectionStrings["myDB"].ConnectionString;
            //this is a simple update, with parameters to pass in values
            string sqlSelect = "update users set status='active' where user_id=" + user_id+ "";

            MySqlConnection sqlConnection = new MySqlConnection(sqlConnectString);
            MySqlCommand sqlCommand = new MySqlCommand(sqlSelect, sqlConnection);

            //sqlCommand.Parameters.AddWithValue("@idValue", HttpUtility.UrlDecode(user_id));

            sqlConnection.Open();
            try
            {
                sqlCommand.ExecuteNonQuery();
                SendEmail(email);
                //after activating account SendEmail is called to send notification to user
            }
            catch (Exception e)
            {
            }
            sqlConnection.Close();

            

        }
        [WebMethod]
        public void DeactivateAccount(int user_id)
        {
            string sqlConnectString = System.Configuration.ConfigurationManager.ConnectionStrings["myDB"].ConnectionString;
            //this is a simple update, with parameters to pass in values
            string sqlSelect = "update users set status='deactive' where user_id=" + user_id + "";

            MySqlConnection sqlConnection = new MySqlConnection(sqlConnectString);
            MySqlCommand sqlCommand = new MySqlCommand(sqlSelect, sqlConnection);

            //sqlCommand.Parameters.AddWithValue("@idValue", HttpUtility.UrlDecode(user_id));

            sqlConnection.Open();
            try
            {
                sqlCommand.ExecuteNonQuery();
            }
            catch (Exception e)
            {
            }
            sqlConnection.Close();
        }

        [WebMethod]
        public void SendEmail(string recipient)
        ///This webmethod will send out an email from cis440parking@asu.edu using googles
        ///SMTP server. Currently it is just used for sending out a confirmation that the 
        ///users account has been approved. Will update code later to add additional messages
        {
            var toAddress = recipient;
            var subject = "Parking account approved";
            var body = "Hello, your parking account has been approved";


            using (MailMessage mail = new MailMessage())
            {

                var smtpAddr = "smtp.gmail.com";
                var portNumber = 587;
                var enableSSL = true;
                var fromAddress = "cis440parking@gmail.com";
                var password = "!!Abracadevs";


                mail.From = new MailAddress(fromAddress);
                mail.To.Add(toAddress);
                mail.Subject = subject;
                mail.Body = body;
                mail.IsBodyHtml = true;
                //mail.Attachments.Add(new Attachment("D:\\TestFile.txt"));//--Uncomment this to send any attachment  
                using (SmtpClient smtp = new SmtpClient(smtpAddr, portNumber))
                {
                    smtp.Credentials = new NetworkCredential(fromAddress, password);
                    smtp.EnableSsl = enableSSL;
                    smtp.Send(mail);

                }
            }
        }

        [WebMethod(EnableSession = true)] 
        public bool LogOn(string user_id, string password)
        { 
      
        bool success = false;

        string sqlConnectString = System.Configuration.ConfigurationManager.ConnectionStrings["myDB"].ConnectionString;
   
        string sqlSelect = "SELECT user_id, admin FROM users WHERE user_id=@idValue and password=@passValue";

 
        MySqlConnection sqlConnection = new MySqlConnection(sqlConnectString);
   
        MySqlCommand sqlCommand = new MySqlCommand(sqlSelect, sqlConnection);

   
        sqlCommand.Parameters.AddWithValue("@idValue", HttpUtility.UrlDecode(user_id));
        sqlCommand.Parameters.AddWithValue("@passValue", HttpUtility.UrlDecode(password));


        MySqlDataAdapter sqlDa = new MySqlDataAdapter(sqlCommand);
    
        DataTable sqlDt = new DataTable();
      
        sqlDa.Fill(sqlDt);
  
        if (sqlDt.Rows.Count > 0)
        {

        Session["user_id"] = sqlDt.Rows[0]["user_id"];
        Session["admin"] = sqlDt.Rows[0]["admin"];
        success = true;
        sqlConnection.Close();
            }

        return success;
        }

        [WebMethod(EnableSession = true)]
        public History[] ViewHistory()
        {
            // var user = Session["user_id"];
            var user = 1;



            DataTable sqlDt = new DataTable("reservations");


            string sqlConnectString = System.Configuration.ConfigurationManager.ConnectionStrings["myDB"].ConnectionString;
            string sqlSelect = "select * from reservations where user_id = " + user + ";";

            MySqlConnection sqlConnection = new MySqlConnection(sqlConnectString);
            MySqlCommand sqlCommand = new MySqlCommand(sqlSelect, sqlConnection);

            MySqlDataAdapter sqlDa = new MySqlDataAdapter(sqlCommand);
            sqlDa.Fill(sqlDt);

            List<History> history = new List<History>();
            for (int i = 0; i < sqlDt.Rows.Count; i++)
            {
                history.Add(new History
                {
                    user_id = Convert.ToInt32(sqlDt.Rows[i]["user_id"]),
                    reservation_ID = Convert.ToInt32(sqlDt.Rows[i]["reservation_id"]),
                    spaceID = Convert.ToInt32(sqlDt.Rows[i]["spaceID"]),
                    date = sqlDt.Rows[i]["date"].ToString()

                });
            }
           

            return history.ToArray();


        }


        [WebMethod]
        public ParkingLot[] ViewParkingOptions(string date)
        {
            // view parking options - see a date selector, taken/ open

            DataTable sqlDt = new DataTable("parkingLots");

            string sqlConnectString = System.Configuration.ConfigurationManager.ConnectionStrings["myDB"].ConnectionString;

            string sqlSelect = "SELECT * FROM reservations WHERE parkingLotName = 'A' AND date = '" + date + "';";

            MySqlConnection sqlConnection = new MySqlConnection(sqlConnectString);
            MySqlCommand sqlCommand = new MySqlCommand(sqlSelect, sqlConnection);

            MySqlDataAdapter sqlDa = new MySqlDataAdapter(sqlCommand);
            sqlDa.Fill(sqlDt);

            List<ParkingLot> parkingLots = new List<ParkingLot>();
            for (int i = 0; i < sqlDt.Rows.Count; i++)
            {
                parkingLots.Add(new ParkingLot
                {
                    reservation_id = Convert.ToInt32(sqlDt.Rows[i]["reservation_id"]),
                    space_id = Convert.ToInt32(sqlDt.Rows[i]["spaceID"]),
                    lotName = sqlDt.Rows[i]["parkingLotName"].ToString(),
                    spotName = sqlDt.Rows[i]["parkingSpotName"].ToString(),
                    date = sqlDt.Rows[i]["date"].ToString(),
                    isReserved = sqlDt.Rows[i]["reserved"].ToString()
                });
            }
            return parkingLots.ToArray();
        }








    }
}