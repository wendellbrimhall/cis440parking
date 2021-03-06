﻿using System;
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
            return "SERVER=107.180.1.16; PORT=3306; DATABASE=" + dbName + "; UID=" + dbID + "; PASSWORD=" + dbPass;
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
                return "Something went wrong, please check your credentials and db name and try again.  Error: " + e.Message;
            }
        }

        [WebMethod]
        public string AddUser(string first, string last, string email, string pass, string pw, string licenseplate)
        {

            ///webmethod to a newuser to the database
            string sqlConnectString = System.Configuration.ConfigurationManager.ConnectionStrings["myDB"].ConnectionString;

            string sqlSelect = "INSERT INTO `abracadevs`.`users` (`first_name`, `last_name`, `email`, `permit`, `status`, `admin`, `license_plate`, `password`)" +
                "VALUES (@fnameValue, @lnameValue, @emailValue, @permitValue, 'pending', '0', @licenseValue, SHA1(@passwordValue));";

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
        {//This function will query the data base for all of the users marked as pending. It will then return all users to JSON so that a table of pending accounts can be created.

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
        public void ActivateAccount(int user_id, string email)
        {
            //This function will activate a users account by updating the database.
            var subject = "Parking account approved";
            var body = "<p>Hello,</p>Your parking account has been approved</p>";


            string sqlConnectString = System.Configuration.ConfigurationManager.ConnectionStrings["myDB"].ConnectionString;
            //this is a simple update, with parameters to pass in values
            string sqlSelect = "update users set status='active' where user_id=" + user_id + "";

            MySqlConnection sqlConnection = new MySqlConnection(sqlConnectString);
            MySqlCommand sqlCommand = new MySqlCommand(sqlSelect, sqlConnection);

          

            sqlConnection.Open();
            try
            {
                sqlCommand.ExecuteNonQuery();
                SendEmail(email, subject, body);
                //after activating account SendEmail is called to send notification to user
            }
            catch (Exception e)
            {
            }
            sqlConnection.Close();



        }
        [WebMethod]
        public void DeactivateAccount(int user_id)
            //This method will deactivate an account. 
        {
            string sqlConnectString = System.Configuration.ConfigurationManager.ConnectionStrings["myDB"].ConnectionString;
            
            string sqlSelect = "update users set status='deactive' where user_id=" + user_id + "";

            MySqlConnection sqlConnection = new MySqlConnection(sqlConnectString);
            MySqlCommand sqlCommand = new MySqlCommand(sqlSelect, sqlConnection);

            

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
        public void SendEmail(string recipient, string subject, string body)
        ///This webmethod will send out an email from cis440parking@asu.edu using googles
        ///SMTP server. 
       
        {
            var toAddress = recipient;
           


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
                
                using (SmtpClient smtp = new SmtpClient(smtpAddr, portNumber))
                {
                    smtp.Credentials = new NetworkCredential(fromAddress, password);
                    smtp.EnableSsl = enableSSL;
                    smtp.Send(mail);

                }
            }
        }

        [WebMethod(EnableSession = true)]
        public bool LogOn(string email, string password)
        //This web method log the user in using their eamil and password 
        {

            bool success = false;
            string sqlConnectString = System.Configuration.ConfigurationManager.ConnectionStrings["myDB"].ConnectionString;
            string sqlSelect = "SELECT * FROM users WHERE email=@emailValue and password=SHA1(@passValue)";

            MySqlConnection sqlConnection = new MySqlConnection(sqlConnectString);
            MySqlCommand sqlCommand = new MySqlCommand(sqlSelect, sqlConnection);

            sqlCommand.Parameters.AddWithValue("@emailValue", HttpUtility.UrlDecode(email));
            sqlCommand.Parameters.AddWithValue("@passValue", HttpUtility.UrlDecode(password));

            MySqlDataAdapter sqlDa = new MySqlDataAdapter(sqlCommand);
            DataTable sqlDt = new DataTable("Account");
            sqlDa.Fill(sqlDt);

            if (sqlDt.Rows.Count > 0)
            {
                //Session variables
                Session["user_id"] = sqlDt.Rows[0]["user_id"];
                Session["admin"] = sqlDt.Rows[0]["admin"];
                Session["permit"] = sqlDt.Rows[0]["permit"];
                Session["first_name"] = sqlDt.Rows[0]["first_name"];
                Session["email"] = sqlDt.Rows[0]["email"];
                Session["last_name"] = sqlDt.Rows[0]["last_name"];
                Session["license_plate"] = sqlDt.Rows[0]["license_plate"];
                Session["twitter"] = sqlDt.Rows[0]["twitter"];

                success = true;

            }

            return success;
        }

        [WebMethod(EnableSession = true)]
        public History[] ViewHistory()
        {
            //This function uses the session object to get the current users  user_id so that it can query the data base and get all of the parking records for the user.
            var user = Session["user_id"];

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
                    date = sqlDt.Rows[i]["date"].ToString(),
                    parkingSpotName = sqlDt.Rows[i]["parkingSpotName"].ToString()


                });
            }


            return history.ToArray();


        }


        [WebMethod(EnableSession = true)]
        public ParkingLot[] ViewParkingOptions(string date)
        {
            // view parking options - see a date selector, taken/ open

            var id = Session["user_id"];
            var permit = Session["permit"];

            DataTable sqlDt = new DataTable("parkingLots");

            string sqlConnectString = System.Configuration.ConfigurationManager.ConnectionStrings["myDB"].ConnectionString;

            string sqlSelect = "SELECT * FROM reservations WHERE parkingLotName = '" + permit + "' AND date = '" + date + "';";

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

        [WebMethod(EnableSession = true)]
        public int GetSessionData()
        {
            var id = Convert.ToInt32(Session["user_id"]);
            var ad = Session["admin"];
            var p = Session["permit"];


            return id;

        }

        [WebMethod(EnableSession = true)]
        public bool GetReservation(string reservation_id )
            {

            string sqlConnectString = System.Configuration.ConfigurationManager.ConnectionStrings["myDB"].ConnectionString;
            var user_id = Session["user_id"];

            string sqlSelect = "UPDATE `abracadevs`.`reservations` SET `user_id` = '" + user_id + "', `reserved` = '1' WHERE (`reservation_id` = '"+reservation_id+"');";

            MySqlConnection sqlConnection = new MySqlConnection(sqlConnectString);
            MySqlCommand sqlCommand = new MySqlCommand(sqlSelect, sqlConnection);

            sqlCommand.Parameters.AddWithValue("@idValue", HttpUtility.UrlDecode(reservation_id));

            sqlConnection.Open();
            try
            {
                sqlCommand.ExecuteNonQuery();

                SendReservationConfirmation(reservation_id);
                //after the reservation is made, the SendReservationConfirmation function is called to notify the user that their spot has been reserved
                return true;
            }
            catch (Exception e)
            {
                return false;
            }
            sqlConnection.Close();

            }

        [WebMethod(EnableSession = true)]
        public void SendReservationConfirmation(string reservation_id)
        {// This function will query the data base to get the info about a parking reservation so that information can be put into a string and emailedto the user
            string sqlConnectString = System.Configuration.ConfigurationManager.ConnectionStrings["myDB"].ConnectionString;

            string sqlSelect = "Select reservations.parkingSpotName, reservations.parkingLotName, reservations.date, users.first_name, users.email From reservations right join users on reservations.user_id = users.user_id Where reservations.reservation_id = " + reservation_id + ";";

            MySqlConnection sqlConnection = new MySqlConnection(sqlConnectString);
            MySqlCommand sqlCommand = new MySqlCommand(sqlSelect, sqlConnection);
            MySqlDataAdapter sqlDa = new MySqlDataAdapter(sqlCommand);
            DataTable sqlDt = new DataTable("Account");

            sqlDa.Fill(sqlDt);

                
                var first_name = sqlDt.Rows[0]["first_name"].ToString();
                var email = sqlDt.Rows[0]["email"].ToString();
                var parkingSpotName = sqlDt.Rows[0]["parkingSpotName"].ToString();
                var parkingLotName = sqlDt.Rows[0]["parkingLotName"].ToString();
                var date = sqlDt.Rows[0]["date"].ToString();

            var subject = "Parking Spot Reservered";
            var body = "<p>" + first_name + "</p><br><p>This email is confirming that you have reserved parking spot "+ parkingSpotName + " in Lot "+ parkingLotName+" on " + date +". </p> <p>Thank you for using AbracaParking!</p>";

            SendEmail(email, subject, body);

        }

        [WebMethod(EnableSession = true)]
        // This method logs off the user
        public bool LogOut()
        {
            //log off session
            Session.Abandon();
            return true;
        }


        [WebMethod(EnableSession = true)]
        public bool CheckAdmin()
        {// this function is called to check if the user is an admin.

            var admin = Convert.ToInt32(Session["admin"]);
            if (admin == 1)
            {
                return true;

            }

            else
            {
                return false;
            }


        }

        [WebMethod(EnableSession = true)]
        public void AddReport(string resID, string text)
        {//Function to add a reported issue to the data base. After database has been updated, the SendReportConfirmation function is called to notify the user that their reported issue was received.

            var user = Session["user_id"];


            string sqlConnectString = System.Configuration.ConfigurationManager.ConnectionStrings["myDB"].ConnectionString;

            //this is a simple update, with parameters to pass in values
            string sqlSelect = "INSERT INTO `abracadevs`.`reported_issues` (`user_id`, reservation_id, `text`, `origin`) VALUES ('1', '" + resID + "', '" + text + "', 'Web');";

            MySqlConnection sqlConnection = new MySqlConnection(sqlConnectString);
            MySqlCommand sqlCommand = new MySqlCommand(sqlSelect, sqlConnection);

            //sqlCommand.Parameters.AddWithValue("@idValue", HttpUtility.UrlDecode(user_id));

            sqlConnection.Open();
            try
            {
                sqlCommand.ExecuteNonQuery();
                SendReportConfirmation(resID, text);
            }
            catch (Exception e)
            {
            }
            sqlConnection.Close();

        }


        [WebMethod(EnableSession = true)]
        public Report[] ViewReports()
        {
            var user = Session["user_id"];

            DataTable sqlDt = new DataTable("reported_issues");


            string sqlConnectString = System.Configuration.ConfigurationManager.ConnectionStrings["myDB"].ConnectionString;
            string sqlSelect = "select * from reported_issues";

            MySqlConnection sqlConnection = new MySqlConnection(sqlConnectString);
            MySqlCommand sqlCommand = new MySqlCommand(sqlSelect, sqlConnection);

            MySqlDataAdapter sqlDa = new MySqlDataAdapter(sqlCommand);
            sqlDa.Fill(sqlDt);

            List<Report> report = new List<Report>();
            for (int i = 0; i < sqlDt.Rows.Count; i++)
            {
                report.Add(new Report
                {
                    report_id = Convert.ToInt32(sqlDt.Rows[i]["report_id"]),
                    user_id = Convert.ToInt32(sqlDt.Rows[i]["user_id"]),


                    text = sqlDt.Rows[i]["text"].ToString(),
                    origin = sqlDt.Rows[i]["origin"].ToString(),



                });
            }


            return report.ToArray();


        }



        [WebMethod(EnableSession = true)]
        public void SendReportConfirmation(string reservation_id, string text)
        {//This function gets all of the data for a user and a reservation so that it can be packaged and send to the SendEmail function.
            string sqlConnectString = System.Configuration.ConfigurationManager.ConnectionStrings["myDB"].ConnectionString;


            string sqlSelect = "Select reservations.parkingSpotName, reservations.parkingLotName, reservations.date, users.first_name, users.email From reservations right join users on reservations.user_id = users.user_id Where reservations.reservation_id = " + reservation_id + ";";


            MySqlConnection sqlConnection = new MySqlConnection(sqlConnectString);
            MySqlCommand sqlCommand = new MySqlCommand(sqlSelect, sqlConnection);
            MySqlDataAdapter sqlDa = new MySqlDataAdapter(sqlCommand);
            DataTable sqlDt = new DataTable("Account");

            sqlDa.Fill(sqlDt);


            var first_name = sqlDt.Rows[0]["first_name"].ToString();
            var email = sqlDt.Rows[0]["email"].ToString();
            var parkingSpotName = sqlDt.Rows[0]["parkingSpotName"].ToString();
            var parkingLotName = sqlDt.Rows[0]["parkingLotName"].ToString();
            var date = sqlDt.Rows[0]["date"].ToString();

            var subject = "Parking issue reported";
            var body = "<p>" + first_name + "</p><br><p>This email is confirming that we have received your complaint about your reservation for parking spot " + parkingSpotName + " in Lot " + parkingLotName + " on " + date + ". We will investigate this issue right away. You will find a copy of the complaint below.</p><p> <i>" + text + "</i></p> <p>Thank you for using AbracaParking!</p>";

            SendEmail(email, subject, body);

        }


        [WebMethod(EnableSession = true)]
        public Account[] ViewAccountInfo()
        {
            // view parking options - see a date selector, taken/ open

            var id = Session["user_id"];
            var first_name = Session["first_name"];
            var last_name = Session["last_name"];
            var email = Session["email"];  
            var license_plate = Session["license_plate"];
            var twitter = Session["twitter"];
            var permit = Session["permit"];

            DataTable sqlDt = new DataTable("user");

            string sqlConnectString = System.Configuration.ConfigurationManager.ConnectionStrings["myDB"].ConnectionString;

            string sqlSelect = "SELECT * FROM users WHERE user_id = '" + id + "';";
            //string sqlSelect = "SELECT * FROM users WHERE user_id = '79';";

            MySqlConnection sqlConnection = new MySqlConnection(sqlConnectString);
            MySqlCommand sqlCommand = new MySqlCommand(sqlSelect, sqlConnection);

            MySqlDataAdapter sqlDa = new MySqlDataAdapter(sqlCommand);
            sqlDa.Fill(sqlDt);

            List<Account> accountInfo = new List<Account>();
            for (int i = 0; i < sqlDt.Rows.Count; i++)
            {
                accountInfo.Add(new Account
                {
                    user_id = Convert.ToInt32(sqlDt.Rows[i]["user_id"]),
                    password = sqlDt.Rows[i]["password"].ToString(),
                    first_name = sqlDt.Rows[i]["first_name"].ToString(),
                    last_name = sqlDt.Rows[i]["last_name"].ToString(),
                    email = sqlDt.Rows[i]["email"].ToString(),
                    permit = sqlDt.Rows[i]["permit"].ToString(),
                    license_plate = sqlDt.Rows[i]["license_plate"].ToString(),
                    twitter = sqlDt.Rows[i]["twitter"].ToString()
                });
            }
            return accountInfo.ToArray();

        }



        [WebMethod(EnableSession = true)]
        public string UpdateAccount(string first_name, string last_name, string email, string license_plate, string twitter,  string permit) 
        {
            var id = Session["user_id"];

            string sqlConnectString = System.Configuration.ConfigurationManager.ConnectionStrings["myDB"].ConnectionString;

            string sqlSelect = "UPDATE users set first_name='"+ first_name + "', last_name='"+ last_name + "', email='" + email + "', " +
                                "license_plate='" + license_plate + "', twitter='"+twitter+ "', permit='" + permit+ "' where user_id="+ id+";";

            MySqlConnection sqlConnection = new MySqlConnection(sqlConnectString);
            MySqlCommand sqlCommand = new MySqlCommand(sqlSelect, sqlConnection);


            //sqlCommand.Parameters.AddWithValue("@firstNameValue", HttpUtility.UrlDecode(first_name));
            //sqlCommand.Parameters.AddWithValue("@lastNameValue", HttpUtility.UrlDecode(last_name));
            //sqlCommand.Parameters.AddWithValue("@emailValue", HttpUtility.UrlDecode(email)); 
            //sqlCommand.Parameters.AddWithValue("@licensePlateValue", HttpUtility.UrlDecode(license_plate));
            //sqlCommand.Parameters.AddWithValue("@twitterValue", HttpUtility.UrlDecode(twitter));
            //sqlCommand.Parameters.AddWithValue("@permitValue", HttpUtility.UrlDecode(permit));

            sqlConnection.Open();

            try
            {
                sqlCommand.ExecuteNonQuery();

               
                Session["first_name"] = first_name;
                Session["last_name"] = last_name;
                Session["email"] = email;
                Session["license_plate"] = license_plate;
                Session["twitter"] = twitter;
                Session["permit"] = permit;

                return "Success!";
            }
            catch (Exception e)
            {

                var str = e.ToString();
                return str;
            }
            sqlConnection.Close();

        }


    }
}