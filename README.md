# <img src="/asserts/HS4%20128px.png" width="32px" height="32px"> Z-Wave Parameters Plugin
Homeseer 4 plugin to view & update Z-Wave device parameters. The parameter information is fetched from [open z-wave database](https://www.opensmarthouse.org/zwavedatabase/). It uses Z-Wave plugin to get and set the parameter of the device.

## Build State

[![Build Release](https://github.com/dk307/HSPI_ZWaveParameters/actions/workflows/buildrelease.yml/badge.svg)](https://github.com/dk307/HSPI_ZWaveParameters/actions/workflows/buildrelease.yml)
[![Unit Tests](https://github.com/dk307/HSPI_ZWaveParameters/actions/workflows/tests.yml/badge.svg)](https://github.com/dk307/HSPI_ZWaveParameters/actions/workflows/tests.yml)
[![Coverage](https://sonarcloud.io/api/project_badges/measure?project=dk307_HSPI_ZWaveParameters&metric=coverage)](https://sonarcloud.io/summary/new_code?id=dk307_HSPI_ZWaveParameters)
[![Quality Gate Status](https://sonarcloud.io/api/project_badges/measure?project=dk307_HSPI_ZWaveParameters&metric=alert_status)](https://sonarcloud.io/summary/new_code?id=dk307_HSPI_ZWaveParameters)

## Features

* The plugin creates an extra page for the Z-Wave devices with the ability to view & update device parameters.
* The page contains a link to the Open Z-Wave database site for the device. You can view description, inclusion\exclusion information & manual on that site.
* The page shows Z-Wave parameters for the device. The listening device's parameter values are loaded on page load. There is the ability to refresh all or an individual parameter.
* Ability to update Z-Wave parameter for the device.
* Offline database is bundled with the plugin so that it does not need an internet connection for operation.
* Online Z-Wave database can be used directly by changing the plugin settings. 

## Device Page

<img src="/asserts/Page.png">

## Plugin Settings

<img src="/asserts/Settings.png">
