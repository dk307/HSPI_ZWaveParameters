#  <img src="/asserts/HS4%20128px.png" width="32px" height="32px">  Z-Wave Parameters Plugin
Homeseer 4 plugin to view & update Z-Wave device parameters. The parameter information is fetched from [open z-wave database](https://www.opensmarthouse.org/zwavedatabase/).

Build State
-----------
[![Build Release](https://github.com/dk307/HSPI_ZWaveParameters/actions/workflows/buildrelease.yml/badge.svg)](https://github.com/dk307/HSPI_ZWaveParameters/actions/workflows/buildrelease.yml)
[![Unit Tests](https://github.com/dk307/HSPI_ZWaveParameters/actions/workflows/tests.yml/badge.svg)](https://github.com/dk307/HSPI_ZWaveParameters/actions/workflows/tests.yml)

Features
-----------
* The plugin creates an extra page for the Z-Wave devices with ability to view & update device parameters.
* The page contains a link to Open Z-Wave database site for device. You can view description, inclusion\exclusion information & manual on that site.
* The page shows Z-Wave parameters for a device. Listening device's parameter values are loaded on page load. There is ability to refresh all or an individual parameter.
* Ability to update any Z-Wave parameter for the device.
* Offline database is bundled with plugin so that it does not need internet connection for operation.
* Online Z-Wave database can be used directly by change the plugin settings. 


Page
----------
<img src="/asserts/Page.png">


Settings
----------
<img src="/asserts/Settings.png">
