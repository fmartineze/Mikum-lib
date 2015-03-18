mikum-lib
=========

A c# client library implementation for the Mikrotik RouterOS API & USER MANAGER Module. 

This project provides a c# client to manipulate Mikrotik routers using the remote API. This library is oriented to automate and make easy to access to the "USER MANAGER" module.

Versions
--------

** Current version is 1.0.0 **


Contributing
------------

We welcome contributions, be it bug fixes or other improvements. If you fix or change something, please submit a pull request. If you want to report a bug, please open an issue. 


Examples
========

These examples should illustrate how to use this library. Please note that I assume that the user is proficient in configure correctly the user manager module into Mikrotik Routerboard.

Example 1: Connect, Login and reboot it. 

```c#
mkusermanager mkum = new mkusermanager("serverip", "8728", "admin", "mypassword");
if (mkum.Connect() && mkum.Login()){
 mkum.Send("/system/reboot", true);
}
```

Example 2: Retrieve Profile list

```c#
mkusermanager mkum = new mkusermanager("serverip", "8728", "admin", "mypassword");
if (mkum.Connect() && mkum.Login()){
  foreach (string ProfileName in mkum.LoadProfiles()) {
	MessageBox.Show(ProfileName);
  }
}
mkum.Disconect(); 

```

References
==========

The RouterOS API is documented here: http://wiki.mikrotik.com/wiki/Manual:API

Licence
=======

This library is released under the Apache 2.0 licence. See the LICENSE.md file