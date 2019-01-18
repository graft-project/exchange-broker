# Exchange-broker
## **GRAFT Exchange Broker**

Exchange Broker - a GRAFT protocol extension hosted on a supernode or a group of supernodes and hosted by the supernode operator. Exchange broker temporary implements special additional features that cannot be automatically executed by a fully decentralized network and/or require special regulation framework.

**_Hardware / Systems Requirement: Minimum hardware requirements include:_**
 
 **OS:** Ubuntu **18.04** LTS Bionic

Name|Build Requirments	|Run Requirements
----|-------------------|----------------
RAM, GB |	8	|2
CPU, Cores	|2	|2
Storage, GB	|100	|100



_Note:_ In order to GraftNode (also called the cryptonode) work properly 28680 (P2P) port should be opened for incoming and outgoing traffic. If you are using other ports, please, make sure that you are open P2P port of the cryptonode.

**_Software environment:_**
- OS Linux, version - Needs Ubuntu 18.04 LTS Bionic (you have to install it yourself), Connections established from the Node are using P2P network. In order to work properly 28680 port should be opened for incoming and outgoing traffic.
- SMTP Server credentials (you have to do it yourself)
- MySQL server, version - was tested with 8.0.13
- .Net Core 2.1 SDK  
- Git
- Nginx 
- 2 Bitcoin Wallet in testnet network - for internal transaction and for test-buyer (we propose for using App Copay).
- 2 Ethereum wallet in Ropsten network - for internal transaction and for test-buyer (we propose for using App Walleth).  
- 1 Graft wallet in RTA testnet network (we propose for using our App Graft Wallet).

# Install Prerequisites


1. Install **git** (if not installed already):
```
  sudo apt-get install -y git
```
2. Install **.Net Core 2.1 SDK** :

>_You don`t need this step if you created this folder for Payment Gateway_

2.1. Open a command prompt and run the following commands:
```
wget -q https://packages.microsoft.com/config/ubuntu/18.04/packages-microsoft-prod.deb
sudo dpkg -i packages-microsoft-prod.deb
``` 
2.2. Update the products available for installation, then install the .NET SDK.

In your command prompt, run the following commands:
```
sudo apt-get install apt-transport-https
sudo apt-get update
sudo apt-get install dotnet-sdk-2.1
```
3.   Install Mysql server for your platform :

>If database was installed for Payment Gateway you need doing only command marked bold.

3.1. Update your package index:
```
sudo apt update
```
3.2. Install the mysql-server package:
```
sudo apt install mysql-server
```
3.3. Run the security script:
```
sudo mysql_secure_installation
```
This will take you through a series of prompts where you can make some changes to your MySQL installation security options. 
- The first prompt will ask whether you’d like to set up the Validate Password Plugin, which can be used to test the strength of your MySQL password. Type y and ENTER to enable it. You will also be prompted to choose a level from 0–2 for how strict the password validation will be.  Choose 1 and hit ENTER to continue.
- The next prompt will be to set a password for the MySQL root user. Enter and then confirm a secure password of your choice.
- From there, you can press Y and then ENTER to accept the defaults for all the subsequent questions. This will remove some anonymous users and the test database, disable remote root logins, and load these new rules so that MySQL immediately respects the changes you have made.

3.4. Test mysql is running:
```
sudo systemctl status mysql
```
3.5. If MySQL isn't running, you can start it with:
```
sudo systemctl start mysql
```
**3.6. Login to mysql as root (password was created in 3.3):**
```
 sudo mysql -u root -p 
```
You stay in mysql>

3.7. List databases:
```
 show databases;
```
**3.8. Create database and user:**

create new database:
```
create database <DB name for ExchangeBroker>
```
For example:
```
create database eb_test;
```
3.9. Create a new user and give it a strong password:
```
CREATE USER '<username>'@'localhost' IDENTIFIED BY '<user password>';
```
For example:
```
CREATE USER 'user1'@'localhost' IDENTIFIED BY 'User_001';
```
**3.10. Grant new user the appropriate privileges for database:**
```
GRANT ALL PRIVILEGES ON eb_test . * TO '<username>'@'localhost';
```
For example:
```
GRANT ALL PRIVILEGES ON eb_test . * TO 'user1'@'localhost';
```
**3.11. Each time you update or change a permission be sure to use the Flush Privileges command:
```
FLUSH PRIVILEGES;
```
Check database:
```
show databases;
```
Quit MySql:**
```
exit
```
Enter to MySql with new user:
```
sudo mysql -u <username> pg_test -p;
sudo mysql -u user1 pg_test -p;
```
Check tables of database (you must have empty set):
```
show tables;
```
Quit MySql:
```
exit
```
Include MySQL to  autorun:
```
sudo systemctl enable mysql 
```
