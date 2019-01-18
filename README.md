# Exchange-broker
## **GRAFT Exchange Broker**

**_Exchange Broker_** - a GRAFT protocol extension hosted on a supernode or a group of supernodes and hosted by the supernode operator. Exchange broker temporary implements special additional features that cannot be automatically executed by a fully decentralized network and/or require special regulation framework.

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


### 1. Install **git** (if not installed already):
```
  sudo apt-get install -y git
```
### 2. Install **.Net Core 2.1 SDK** :

>**_You don`t need this step if you created this folder for Payment Gateway_**

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
### 3. Install **Mysql server** for your platform :

>**_If database was installed for Payment Gateway you need doing only command marked bold._**

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
_For example:_
```
create database eb_test;
```
3.9. Create a new user and give it a strong password:
```
CREATE USER '<username>'@'localhost' IDENTIFIED BY '<user password>';
```
_For example:_
```
CREATE USER 'user1'@'localhost' IDENTIFIED BY 'User_001';
```
**3.10. Grant new user the appropriate privileges for database:**
```
GRANT ALL PRIVILEGES ON eb_test . * TO '<username>'@'localhost';
```
_For example:_
```
GRANT ALL PRIVILEGES ON eb_test . * TO 'user1'@'localhost';
```
**3.11. Each time you update or change a permission be sure to use the Flush Privileges command:**
```
FLUSH PRIVILEGES;
```
**Check database:**
```
show databases;
```
**Quit MySql:**
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
### 4.   **Nginx** setup
> **_If nginx was installed for Payment Gateway you need doing only command marked bold._**

4.1. Install nginx
```
sudo apt install nginx
```
4.2. Check that nginx is running:
```
sudo systemctl status nginx
```
4.3. If you need to start nginx:
```
sudo systemctl start nginx
```
4.4. To enable the service to start up at boot:
```
sudo systemctl enable nginx
```
**4.5.  Creating Self-signed Certificates:**
```
sudo openssl req -x509 -nodes -days 365 -newkey rsa:2048 -keyout /etc/ssl/private/<name+domain name >.key -out /etc/ssl/certs/eb-test.graft.network.crt
```
_For example:_
For eb-test.graft.network
```
sudo openssl req -x509 -nodes -days 365 -newkey rsa:2048 -keyout /etc/ssl/private/eb-test.graft.network.key -out /etc/ssl/certs/eb-test.graft.network.crt
```
You will be asked a few questions about our server in order to embed the information correctly in the certificate.
Fill out the prompts appropriately. 

While we are using OpenSSL, we should also create a strong Diffie-Hellman group, which is used in negotiating Perfect Forward Secrecy with clients.
```
sudo openssl dhparam -out /etc/nginx/ssl/dh2048.pem 2048
```
**4.6. Make configuration files for <name> in nginx:**
```
Go to /etc/nginx/conf.d:
cd /etc/nginx/conf.d
```
Create files <name + domain name>.conf 

_For example,  for eb-test.graft.network.conf:_
```
sudo nano eb-test.graft.network.conf
```
Insert next information:
_For our example  eb-test.graft.network.conf:_
```
upstream localhost-5002 {
keepalive 64;
  server 127.0.0.1:5002 max_fails=2 fail_timeout=5s;
}

upstream localhost-5003 {
keepalive 64;
  server 127.0.0.1:5003 max_fails=2 fail_timeout=5s;
}

server {
        listen 80;
        server_name eb-test.graft.network;
        access_log  /var/log/nginx/eb-test.graft.network.access.log;

        location /.well-known/ {
            alias /var/www/eb-test.graft.network/.well-known/;
        }

        location / {
            proxy_pass       http://localhost-5002;
            proxy_set_header Host      $host;
            proxy_set_header X-Real-IP $remote_addr;
#            return 301 https://$host$request_uri;
        }
}

server {
       listen 443 ssl http2;

       server_name eb-test.graft.network;
       access_log  /var/log/nginx/eb-test.graft.network.ssl.access.log;

       location / {
              proxy_pass       https://localhost-5003;
              proxy_set_header Host      $host;
              proxy_set_header X-Real-IP $remote_addr;
              proxy_set_header X-Forwarded-Proto $scheme;
        }

    ssl_certificate      /etc/ssl/certs/eb-test.graft.network.crt;
    ssl_certificate_key  /etc/ssl/private/eb-test.graft.network.key;
    ssl_dhparam /etc/nginx/ssl/dh2048.pem;
    ssl_session_cache shared:SSL:60m;
    ssl_session_timeout 1d;
    ssl_session_tickets off;

    ssl_protocols TLSv1 TLSv1.1 TLSv1.2;
    ssl_ciphers 'ECDHE-ECDSA-CHACHA20-POLY1305:ECDHE-RSA-CHACHA20-POLY1305:ECDHE-ECDSA-AES128-GCM-SHA256:ECDHE-RSA-AES128-GCM-SHA256:ECDHE-ECDSA-AES256-GCM-SHA384:ECDHE-RSA-AES256-GCM-SHA384:DHE-RSA-AES128-GCM-SHA256:DHE-RSA-AES256-GCM-SHA384:ECDHE-ECDSA-AES128-SHA256:ECDHE-RSA-AES128-SHA256:ECDHE-ECDSA-AES128-SHA:ECDHE-RSA-AES256-SHA384:ECDHE-RSA-AES128-SHA:ECDHE-ECDSA-AES256-SHA384:ECDHE-ECDSA-AES256-SHA:ECDHE-RSA-AES256-SHA:DHE-RSA-AES128-SHA256:DHE-RSA-AES128-SHA:DHE-RSA-AES256-SHA256:DHE-RSA-AES256-SHA:ECDHE-ECDSA-DES-CBC3-SHA:ECDHE-RSA-DES-CBC3-SHA:EDH-RSA-DES-CBC3-SHA:AES128-GCM-SHA256:AES256-GCM-SHA384:AES128-SHA256:AES256-SHA256:AES128-SHA:AES256-SHA:DES-CBC3-SHA:!DSS';
    ssl_prefer_server_ciphers on;
}
```
After that, press Ctrl-X and Y and ENTER

**4.7.  Restart nginx:**
```
sudo systemctl restart nginx 
```

# Installation

### 5. Install **Exchange Broker**:

Create a folder to store the sources (for example src) and clone the repositories into this folder:

5.1. Create folder (you don`t need this step if you created this folder for Payment Gateway):
```
mkdir  src
```
5.2. Go to folder src:
```
cd src
```
5.3. Download Exchange Broker:
```
git clone --recurse-submodules  https://github.com/graft-project/ExchangeBroker.git
```
5.4. Go to folder src/ExchangeBroker/ExchangeBroker:
```
cd ExchangeBroker/ExchangeBroker
```
5.5. Build EchangeBroker : 
```
dotnet publish  -c release -v d -o "<path to build>" --framework netcoreapp2.1 --runtime linux-x64 ExchangeBroker.csproj
```
_For example:_
```
dotnet publish  -c release -v d -o "/home/ubuntu/graft/eb" --framework netcoreapp2.1 --runtime linux-x64 ExchangeBroker.csproj
```

### 6. Download Geth Node (optional, to support Ethereum):

Download binary code Geth Node from (https://github.com/ethereum/go-ethereum/releases) :

```
cd <path to build>
mkdir  <path to store Geth Node>
cd <path to store Geth Node>
```
_For example:_
```
cd /home/ubuntu/graft/
mkdir  ethnode
cd  /home/ubuntu/graft/ethnode
wget https://gethstore.blob.core.windows.net/builds/geth-linux-amd64-1.8.20-24d727b6.tar.gz
tar -xvf geth-linux-amd64-1.8.20-24d727b6.tar.gz
```

### 7.  Download and Build  Graft Node : 

You have to do it with (https://github.com/graft-project/graft-ng/wiki/Alpha-RTA-Testnet-Install-&-Usage-Instruction)

> Build Graft Supernode - >Graft Node Configuration -> Graft SuperNode configuration 

### 8. Configure settings:

All settings related to the  application stored in the config file ‘appsettings.json’ located in the root bin directory, 
_for example,_ "/home/ubuntu/graft/eb".
Open this file and add/edit following sections:
```
sudo nano /home/ubuntu/graft/eb/appsettings.json
```
8.1 **Admin** settings for service administrator
```
"Admin": {
    "DefaultPassword": "GRAFT_admin1"
  },
```
where:

 - **GRAFT_admin1** -  default password for service administrator

8.2. **DB** – settings to access previously created database:
```
  "DB": {
    "UserName": "root",
    "Password": "testpass",
    "DbName": "exchange_broker",
    "Server": "127.0.0.1",
    "Port": "3306"
  },
```
where:

- **UserName** - root name for DB
- **Password**  -  root password for DB
- **DbName** - name of DB
- **Server**  - 127.0.0.1
- **Port** - 3306

8.3. **Watcher** – this is internal service responsible for monitoring application state and inform the administrator via email in case of any troubles.
```
  "Watcher": {
    "AdminEmails": "admin@<yourcompany>.com",
    "ErrorEmailSubject": "EB-localhost Error (_service_name_)",
    "WarningEmailSubject": "EB-localhost Warning (_service_name_)",
    "RestoreEmailSubject": "EB-localhost Restore (_service_name_)",
    "CheckPeriod": "10000"
  },
```
where:

- **AdminEmails** - your email.
- **__service_name_** - is a placeholder for the particular service, leave it as it is.
- **CheckPeriod** – interval in milliseconds to perform periodical check of the application state  


8.4. **EthereumService** - settings for Ethereum: 
```
 "EthereumService": {
    "NetworkType": "PublicRTATestnet",
    "EthereumGethNodeUrl": "http://localhost:8545",
    "EthereumPoolWalletPassword": "DefaultPassword",
    "EthereumBrokerWallet": "",
    "EthereumPoolDrainLimit": 0.001
  },
```
	where: 

- **NetworkType** - (MainNet, PublicRTATestnet, PublicTestnet) Operational network. (For PublicRTATestnet, PublicTestnet ETH network is Ropsten).
- **EthereumBrokerWallet** - Ethereum wallet to accept payments.
- **EthereumPoolDrainLimit** - Fund limit that will drain the ETH pool wallet funds to EthereumBrokerWallet. 
- **EthereumGethNodeURI** - Path to Geth node.
- **EthereumPoolWalletPassword** - Exchange broker will create pool of Eth wallets to accommodate simultaneous transactions. This will be the password to new wallets


8.5. **BitcoinService** - settings for Bitcoin: 
```
"BitcoinService": {
    "NetworkType": "PublicRTATestnet", 
    "BitcoinExtPubKeyString":
"tpubDCfbgtNpe7u966FDF5d6E5P12quCovWdqSA3GzYk5BHuPEbYZNPmDzzp5Qx9q3dVyatEzUR23Qc62Ftms1wLQSTPW8nc7eqFM1H7YbviUjY"
  },
```
where:

- **NetworkType** - (MainNet, PublicRTATestnet, PublicTestnet) Operational network. 
- **BitcoinExtPubKeyString** - your desired Bitcoin wallet ext key to accept payments.

8.6. **PaymentService** - settings for Exchange Broker:
```
 "PaymentService": {
    "ExchangeBrokerFee": 0.0075,
    "MaxServiceProviderFee": 0.2,
    "PaymentTimeoutMinutes": 16
  },
```
where:

- **ExchangeBrokerFee** - Fee for exchange operations.
- **MaxServiceProviderFee** - Maximum fee that provider can charge for the transaction.
- **PaymentTimeoutMinutes** - Value in minutes that describes the period where Graft node waits for new transactions before actually performing them.

8.7. **GraftWalletService** - settings for GraftWallet 
```
"GraftWalletService": {
    "RpcUrl": "http://127.0.0.1:28982/"
```
where:

**RpcUrl** - Path to Graft RPC node.

>**_Note: your <graft-wallet-name> should contain funds to enable graft payouts_**

### 9. Install **Exchange Broker**:

9.1.  Go to Exchange broker source directory:

_For example:_
```
cd /home/ubuntu/src/ExchangeBroker/
```
9.2.  Build EchangeBroker : 
```
dotnet publish -v d -o "<your-path-to-publish>" --framework netcoreapp2.1 --runtime <(linux-x64) or other> ExchangeBroker.csproj
```
_For example:_
```
dotnet publish -v d -o "<your-path-to-publish>" --framework netcoreapp2.1 --runtime <(linux-x64) or other> ExchangeBroker.csproj
```
9.3. Populate database with EFCore : 
```
dotnet ef database update
```
### 10. Run Exchange Broker:

10.1. Run Geth Node (for Ethereum) :
```
./geth --testnet [--datadir "<your-data-dir>"] --rpc --rpcapi personal,web3,eth,outbound
```
_For example:_
```
cd /home/ubuntu/graft/ethereum/geth-linux-amd64-1.8.20-24d727b6
./geth --testnet --rpc --rpcapi personal,web3,eth,outbound
```
10.2. Run Graft Node 
```
./graftnoded --testnet --confirm-external-bind
```
_For example:_
```
cd  /home/ubuntu/graft/supernode/BILD/bin   
./graftnoded --testnet --confirm-external-bind
```
10.3. Run Graft RPC :

You have to do it with (https://github.com/graft-project/graft-ng/wiki/Alpha-RTA-Testnet-Install-&-Usage-Instruction)

> **_5)Run Supernode -> Appendix 1. Running Graft Node->Creating a wallet and connecting to the local testnet node_**

_For example:_

Create folder:
```
cd ./grfat/supernode/data
mkdir exchangebroker-wallet
```
Create a new wallet:
```
 	cd home/ubuntu/graft/supernode/BILD/bin
./graft-wallet-cli --generate-new-wallet /home/ubuntu/.graft/supernode/data/exchangebroker-wallet/exchangebroker-wallet --testnet --daemon-address localhost:28681
```


> **_Go to /home/ubuntu/.graft/supernode/exchangebroker-wallet and note wallet address from “exchangebroker-wallet.address.txt”. Request stake amount for your Exchange Broker  by sending  email to alpha@graft.network with your wallet address - we will load up your stake wallet with testnet coins._**


```
cd home/ubuntu/graft/supernode/BILD/bin
./graft-wallet-rpc --testnet --wallet-file /home/ubuntu/.graft/supernode/data/exchangebroker-wallet/exchangebroker-wallet --rpc-bind-port 28982 --password "" --disable-rpc-login --trusted-daemon
```

10.4.  Go to ExchangeBroker publish folder and run :
```
cd <ExchangeBroker publish folder>
 nohup ./ExchangeBroker &
```
_For example:_
```
cd /home/ubuntu/graft/eb
 nohup ./ExchangeBroker &
```

After that Exchange Broker should be ready to accept transactions.


### 11. Verify Installation


11.1.  Open the link https://**name of your site**/DemoTerminalApp

where:

 **name of your site** is name, which you create in ngnix + your domain name.
 
_For example:_
(https://eb-test.graft.network/DemoTerminalApp)

11.2. You should see the screen (pic.1):

![r_2019-01-14_10-49-51](https://user-images.githubusercontent.com/45132833/51415622-23fb9e80-1b7f-11e9-9492-610180ee5349.png)
Pic.1

11.3. Make sure the Bitcoin currency is selected (pic.1, [1]) and click the "Pay" button (pic.1, [2])

11.4. You should see the screen (pic.2):

![2019-01-15_14-04-37](https://user-images.githubusercontent.com/45132833/51415728-818feb00-1b7f-11e9-96e7-e75ff599e3d3.png)
Pic.2

11.5. Make sure the cup of coffee- $1 is selected (pic.2, [1]) and click the "Pay" button (pic.2, [2])

11.6. You should see the screen (pic.3):

![2019-01-15_20-04-52](https://user-images.githubusercontent.com/45132833/51415885-18f53e00-1b80-11e9-93a7-7ef659be30a5.png)
Pic.3

11.7. Enter address of GRAFT Wallet, which will get payment, in the field (pic.3,[1]) and press button (pic.3,[2])
>**_Note: field “Wallet address” (pic.3,[1]) has a red border  if  wallet address is incorrect.
Field “Wallet address” (pic.4) has a blue border  if  wallet address is correct:_**

![r_1](https://user-images.githubusercontent.com/45132833/51416430-30cdc180-1b82-11e9-8552-499ea5622fe9.png)
Pic.4

11.8. You should see the screen (pic.5):

![2019-01-15_21-39-03](https://user-images.githubusercontent.com/45132833/51415901-1c88c500-1b80-11e9-8f45-a56ae2b94035.png)
Pic.5

11.9. information about transaction is displayed on the down side of the screen (pic.5, [2])

11.10. Open your bitcoin wallet, scan qr-code (pic.5, [1]) and pay this payment.

11.11. Wait for transferring  transaction. If transaction is sended:

 you will see info about successful completed transaction on the screen of your PC/laptop (pic.6):

![ok_1](https://user-images.githubusercontent.com/45132833/51415905-23173c80-1b80-11e9-8308-3fb563f7bd29.png)
Pic.6

You will see info about successful sended transaction on the screen of your Wallet (pic.7) and late you will receive info about completed transaction:

![r_2](https://user-images.githubusercontent.com/45132833/51416431-30cdc180-1b82-11e9-8a5b-e815593e1562.png)

Pic.7

11.12. You have a successful result if money from your bitcoin wallet send to your graft wallet.


### 12. Probable errors list:

**_Error # 1_**

![r_3](https://user-images.githubusercontent.com/45132833/51416432-30cdc180-1b82-11e9-96ac-2b9d6579f333.png)

**_Solution:_** Run ExchangeBroker (see 10.4)

**_Error # 2_**

![picturemessage_tqqqjuwi 0j1](https://user-images.githubusercontent.com/45132833/51416240-4db5c500-1b81-11e9-8e82-f655f4128eae.png)

**_Solution:_** check configuration settings (see 8)

**_Error # 3 :  Button “Proceed” is not pressed_**

![r_4](https://user-images.githubusercontent.com/45132833/51416433-30cdc180-1b82-11e9-9b69-2473f1fb6bb3.png)

**_Solution:_** Enter correct wallet address
