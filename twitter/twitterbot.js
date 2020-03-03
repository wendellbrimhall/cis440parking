//Twitterbot for ASU CIS 440 Project 1 - Abracadevs
//Twitterbot allows users to register their twitter handle after signing for out website. Once the user has completed that, they can send a tweet to @cis440asu. If a user
//tweets 1, 2 or 3 twitter bot will reserve the spot for them if it is available and send a back a confirmation tweet to the user. If the spot is not available it will //send a respond tweet and tell the user what other spaces are still open. 
//Twitter bot requires the Twit and mysql npm's in order to run.
console.log("Twitter bot is alive");
var mysql = require('mysql');
var Twit = require('twit');

var T = new Twit({
	  consumer_key: 'vIfsUdJchhi0rw4bC2TZQyoMn'
	, consumer_secret: 'bRk3gTmLZxsOkH4XqWW1qCFKOTCsgK7khCSiKkNmgD7yX6oY3a'
	, access_token: '1227455793416605696-w5doWegyQhxioMRomojdGhcNulntYe'
	, access_token_secret: 'jZa1AA27S73gxGgE9YLCvYkvVxWebmwHucfFjbYgNnYIC'

});

var port = 3306;
var addr = '107.180.1.16';
var conn = mysql.createConnection({
  host: '107.180.1.16',
  user: 'abracadevs',
  password: '!!Abracadevs',  
  });

conn.connect(function(err) {
  		if (err) throw err;
  		console.log('Database connected');
  	});

function getLast(){
	//this function will query the database to get the twitterID the last tweet that was recorded in the database.
  	conn.query("SELECT twitterID FROM abracadevs.twitter ORDER BY ID DESC LIMIT 1;", function (err, result, fields) {
    	if (err) throw err;
    	var  twitID = result[0].twitterID;
    	var newTwitID = Number(twitID) ; 		
       	getNew(newTwitID);
    	//Function ends by calling getNew and passes over the tweetID from the database 					    			      		
    });
  					
}
 	
function getNew (db){
	//this function uses the twitter api to pull the mentions_timelines for @cis440asu. It will retrieve the tweets and then compare the tweetID of the newest tweet
	//to the tweetID saved in the database. 
	T.get('statuses/mentions_timeline',  function(err, data, response) {
  		if(err) throw err; 
  			var saveTweet = 	[
  					data[0].id,
  					data[0].user.screen_name,
  					data[0].user.name,
  					data[0].text
  					]
 					
  	 		if (saveTweet[0]==db){
  	 		//If the id's match, then there are no tweets and nothing further happens
  	 			console.log("No new tweets!");
  	    }
  		else{
  	 		console.log("New Tweet from @"+ data[0].user.screen_name );
  	 		//If the id's do not match, then we have a new tweet. updateDB is passed the tweet info so that the tweet can be stored in the Database, the data received from
  	 		//the twitterapi is then passed to the checkUser function to decide what should be done next

  	 		updateDB(saveTweet);
  	 		checkUser(data);
  	 	}
	});
}

function updateDB(data){
	//This function saves the tweet to the database so that we don't send out multiple replies to the same tweet
  			
  	conn.query("INSERT INTO `abracadevs`.`twitter` (`twitterID`, `twitterHandle`, `name`, `text`) VALUES (" + data[0] + ", '" + data[1] + "', '" + data[2] + "', '" + data[3] + "')", function (err, result, fields) { 	
  			if (err) throw err;		    		
    			 console.log(data[0] + "addded to database");
    });		
}

function checkUser(data){
	//this function is passed the data from twitter. It will then get the twitter screen_name of the user who sent the twet and check the database to see if there 
	//are any users with this twitter handle in the database
	var twitHandle = data[0].user.screen_name.toLowerCase();	
	conn.query("SELECT * from abracadevs.users;", function (err, result, fields) {
  		if (err) throw err;	  
    		console.log(result[0].twitter);
    		var userNotFound = true;
    			   
    			for (var i=0; i < result.length; i++){
    			   		//for loop to run through list of users returned for database to check for matches
    			   	if (result[i].twitter != null) {
    			   		//if a users has registered a twitterHandle on their account then it will be changed to lowercase so that it can be 
    			   		//compared to the twitterHandle in the tweet that was received

    			   		var temphandle = result[i].twitter.toLowerCase();   			   			

    			   		if (temphandle == twitHandle) {
    			   			//if a match is found, the data from twitter will be passed to the checkMsg function with the user_id and permit received from the database.
    			   			//it will then change userNotFound to true and break out of the for loop.

        					console.log('User found: '+ temphandle);      						
        					userNotFound = false;
        					checkMsg(data, result[i].user_id, result[i].permit );
        					break;
        				}
    			   	}    			   		        				
        		}
					
 			   if (userNotFound == true){
 			   	// if userNotFound is still true after the for loop has completed, it means there were no matches. The respond function will be called to respond to the 
 			   	//user informing them that we do not recognize them. 
   			   		console.log('user not found');
    			   	var msg = "Sorry we do not recognize you. Please register your account.";  			   	
    			   	respond(data, msg);
    			}		      		
    });			
}

function checkMsg(data, user_id, permit){
	//this function checks the content of the message received from twitter. Every tweet begins with @cis440asu with a space. For a user to reserve a spot they need
	// to tweet 1, 2, or 3. That means that reservation requests will be a string of 12 chacters. The last character of the string will be sliced and stored in a variable. 
	var msg = data[0].text;
	var msgLen = msg.length;
	var handle = data[0].user.screen_name;
	var handleLen = handle.length;
	var sliceMsg = msg.slice(11);
	var counter = 0;

	if (msgLen == 12){
		// if a tweet is 12 characters long, it is a reservation request. The last character is stored but we need to make sure that it is 1, 2, or 3. These if statements
		//will check to see if is 1, 2, or 3. When it is not one of those number counter will increment. If counter = 3 after these if statements, it means that the user 
		//tweeted something outside of that range.
		if (sliceMsg != '1'){
			counter++}

		if (sliceMsg != '2'){
			counter++
		}
		if(sliceMsg != '3'){
			counter++
		}
		if (counter == 3){
			// If counter == 3 then it means that the user did not enter a valid number to make a reservation. The respond function is passed a message informing the user
			//that what they tweeted is not valid.
		
			console.log(sliceMsg ="is not a valid number");
		 
			var responseMsg = "Sorry but "+sliceMsg+" is not a valid parking spot. Please pick a number between 1-3";
			respond(data, responseMsg);
		}
		else {
			//if counter++ < 3 it means the user tweeted 1, 2, or 3. The checkReserv function is called and has the relevant data passed to check if the users requested
			//parking spot is already reserved
			console.log("Checking to see if spot is reserved.");
			checkReserve(data, user_id, permit, sliceMsg);
		}		

	}
	else {
	// if the length of the tweet is anything besides 12 then it will be handled as a report. 	
		console.log("Making report");
		reportIssue(data, user_id);
	}
}

function reportIssue(data, user_id){
	// This is the function to handle users reporting an issue. It will get todays date and store the information to the database for review by an admin.
	var today = new Date();
	var date = today.getFullYear()+'-'+(today.getMonth()+1)+'-'+today.getDate();
	var text = data[0].text;
	conn.query("INSERT INTO `abracadevs`.`reported_issues` (`user_id`, `date`, `text`, `origin`) VALUES ('"+user_id+"', '"+date+"', '"+text+"', 'twitter');", function (err, result, fields){
			if (err) throw err;
			var responseMsg = "Your issue has been reported. We will look into this issue right away";
			console.log("Issue Reported");
			//The respond function is called to let the user know that we are looking into their issue
			respond(data, responseMsg);
	});
}

function checkReserve(data, user_id, permit, sliceMsg){
	// This function checks to see if the users requested parking spot is open. It will get todays date and then check the database to see if that spot has been reserved
	// today
	var today = new Date();
	var date = today.getFullYear()+'-'+(today.getMonth()+1)+'-'+today.getDate();
	console.log(date);
	var spotName = permit + '0' + sliceMsg;
	console.log("spot requested"+ spotName);
	var spotAvail = false;
	var availableSpots = [];
	var counter = 0;
	var msg = '';

	conn.query("SELECT * FROM abracadevs.reservations WHERE date='"+ date +"' AND parkingLotName='"+permit+"';", function (err, result, fields){
		//query returns results for all parking spots for the users permit with the date of taday
			if (err) throw err;
			
			 for (var i=0; i < result.length; i++){
			 	//This for loop will go through each spot returned. If the users requested spot is available, then the reserve function is called, spotAvail = true, and we 
			 	// break out of the for loop.
			 	if (result[i].parkingSpotName == spotName){
			 		if (result[i].reserved == false){
			 			spotAvail = true;
			 			reserve(data, user_id, spotName, date, result[i].reservation_id);
			 			console.log('Spot available');
			 			break;
			 		}
			 	}
			 	else if(result[i].reserved == false){
			 		//if the spot is available then counter will increment, and the parking spot name will be added to a list
			 			availableSpots.push(result[i].parkingSpotName);	
			 			counter++;				 				 			
			 		}
			}		 
			 if (spotAvail == false){
			 	//if spotAvail is false, then the users requested spot is already reserved. By checking how many times counter incremented, we know how many
			 	//spots are still available. We will use the counter and the availableSpots list to make a message informing the user of what spots are still
			 	//available.
			 	if ( counter == 1){
			 		msg = "Sorry, that spot is not available. However, spot "+ availableSpots[0] +" is still free. ";			 		
			 	}
			 	 if ( counter == 2){
			 	msg = "Sorry, that spot is not available. However, spots "+ availableSpots[0] +" and "+ availableSpots[1] + " are still free. ";
			 	}
			 	if (counter == 0 ) {
			 		
			 		msg = "Sorry, all spots are reserved.";
			 	}
			 	console.log(msg);
			 	//user is sent a response letting them know their spot is unavailable and informing them what spots are left.
			 	respond(data, msg);
			}
			 spotAvail = false;
	});

}

function reserve(data, user_id, spotName, date, reservation_id){
	//this function is called once we have verified the requested spot is available. It will call the database to reserve the spot. Then inform the user that their 
	//reservation was succesful.
	conn.query("UPDATE `abracadevs`.`reservations` SET `user_id` = '"+user_id+"', `reserved` = '1' WHERE (`reservation_id` = '"+reservation_id+"');", function (err, result, fields){
		if (err) throw err;
		console.log("Spot reserved "+spotName+" for "+ user_id);
		var msg = ("Thank you, you have reserved parking spot "+spotName+".");
		respond(data, msg);

	});

}
 
			
function respond(data, msg){
	//this function uses the twitter api to respond to tweets. It will accept data received from the twitter api get request and a message as parameters. It will use
	//the twitter data to reply to the tweet/
	var screen_name =(data[0].user.screen_name);
	
	var r = Math.floor(Math.random()*100);
	//This is for testing only. Puts a random number on tweet so that tweet isn't stopped for being a duplicate.
	var randomizer = "Reply: " + r;

	T.post('statuses/update', { in_reply_to_status_id: data.id_str, status: '@' + screen_name + ' '+ msg+ ' '+ randomizer }, function(err, data, response) {
  	console.log('tweet sent to: @' + screen_name);  
	});
}

setInterval(function(){getLast()}, 10000);
//This function runs the whole process every 20 seconds. It calls the getLast function which will start the process.
// Twitter limits the number of api calls that we can make in a 15 minute period to 75. Setting the interval at 20 seconds ensures that we stay will below this limit and
// twitter bot can continue to run





