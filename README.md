# <img src="/asserts/HS4%20128px.png" width="32px" height="32px"> Z-Wave Parameters Plugin
Homeseer 4 plugin to view & update Z-Wave device parameters. The parameter information is fetched from [open z-wave database](https://www.opensmarthouse.org/zwavedatabase/).

## Build State

[![Build Release](https://github.com/dk307/HSPI_ZWaveParameters/actions/workflows/buildrelease.yml/badge.svg)](https://github.com/dk307/HSPI_ZWaveParameters/actions/workflows/buildrelease.yml)
[![Unit Tests](https://github.com/dk307/HSPI_ZWaveParameters/actions/workflows/tests.yml/badge.svg)](https://github.com/dk307/HSPI_ZWaveParameters/actions/workflows/tests.yml)
[![Codacy Badge](https://app.codacy.com/project/badge/Grade/9aa22e16a28a4b56ab2b03135ba4d57b)](https://www.codacy.com/gh/dk307/HSPI_ZWaveParameters/dashboard?utm_source=github.com&amp;utm_medium=referral&amp;utm_content=dk307/HSPI_ZWaveParameters&amp;utm_campaign=Badge_Grade)

## Features

* The plugin creates an extra page for the Z-Wave devices with the ability to view & update device parameters.
* The page contains a link to the Open Z-Wave database site for the device. You can view description, inclusion\exclusion information & manual on that site.
* The page shows Z-Wave parameters for the device. The listening device's parameter values are loaded on page load. There is the ability to refresh all or an individual parameter.
* Ability to update any Z-Wave parameter for the device.
* Offline database is bundled with the plugin so that it does not need an internet connection for operation.
* Online Z-Wave database can be used directly by changing the plugin settings. 

## Device Page

<img src="/asserts/Page.png">

## Plugin Settings

<img src="/asserts/Settings.png">
