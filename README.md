# AspProject
This is my first ASP.net project, i aim to make a logon system over a encrypted connection
<<WORK IN PROGRESS!>>

Please note: this project is just a personal challenge i am doing, and it is by no means a secure solution for a logon system.

it also uses a local SQL database, so if you want to make it work, you will have to change the connection strings to suit your own envirnoment

this is probably the biggest project i took on by far, and i set some ambitious goals, so there is no guarantee for success


as of now, i ma using a file- based database, that has a table that was created using the following query 

CREATE TABLE User_Auth_Data(
 AuthID INT NOT NULL PRIMERY KEY,
 IpAddress VARCHAR(29) NOT NULL,
 Username VARCHAR(10) NOT NULL,
 ExpDate VARCHAR(33)  NOT NULL
)

CREATE TABLE User_Auth_Data(
 AuthID INT NOT NULL PRIMERY KEY, - int is used because the cryptographic RNG can only generate INT 32 which best suits this type
 IpAddress VARCHAR(39) NOT NULL,- the longest IPV6 address is 39 characters
 Username VARCHAR(10) NOT NULL, - 10 characters since the original has 10 characters length, usernames are used here to search for them to de-authenticate them when a password is changed 
 ExpDate VARCHAR(33)  NOT NULL - the longest value returned by DateTime is 33 characters long, whilst i am working on making th eprecision go down for efficiency, i will implement it, since this is only personal project

)
