# Web API Tester plugin for [OpenTAP](https://www.opentap.io/)

## What is OpenTAP?
OpenTAP is an Open Source project for fast and easy development and execution of automated tests. 
OpenTAP is built with simplicity, scalability and speed in mind and is based on an extendable architecture that leverage .NET Core. 
OpenTAP offers a range of sequencing functionality and infrastructure that makes it possible for you to quickly develop plugins tailored for your automation needs – plugins that can be shared with the OpenTAP community throught the OpenTAP package repository. 
Read more about the OpenTAP project [here](https://gitlab.com/OpenTAP/opentap).

### Getting OpenTAP

If you are looking to use OpenTAP, you can get pre-built binaries at [opentap.io](https://opentap.io). 

Using the OpenTAP CLI you are now able to download plugin packages from the OpenTAP package repository.

To list and install plugin packages do the following in the command prompt: 
```
cd %TAP_PATH%
tap package list
tap package install <Package Name>
```

We recommend that you download the Software Development Kit, or simply the Developer’s System Community Edition provided by Keysight Technologies. The Developer System is a bundle that contain the SDK as well as a graphical user interface and result viewing capabilities. It can be installed by typing the following:
```
tap package install "Developer's System CE" -y
```

## Installing the Web API Tester plugin

The plugin will soon be available as a package on [OpenTAP package repository](https://packages.opentap.io), but until then you can:

```
git clone https://gitlab.com/Denrask/opentap-apitest.git
cd opentap-apitest
dotnet build WebApiTester.sln
```

This will automatically build to your %TAP_PATH%/Packages/ApiTest folder.

## Using the Web API Tester Plugin

### TestPlan development

### Convert from an exported Postman Testcollection v2.1

### Add Web API Server DUT from CLI

### Running the tests in a CI environment