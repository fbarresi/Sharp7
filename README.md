# ![Sharp7](https://raw.githubusercontent.com/fbarresi/sharp7/master/doc/images/logo.jpg)

[![Build status](https://ci.appveyor.com/api/projects/status/2i77qfjjq8aep50b?svg=true)](https://ci.appveyor.com/project/fbarresi/sharp7)
[![Codacy Badge](https://api.codacy.com/project/badge/Grade/4ff75e759a66416a84052769a71b70c6)](https://www.codacy.com/manual/fbarresi/Sharp7?utm_source=github.com&amp;utm_medium=referral&amp;utm_content=fbarresi/Sharp7&amp;utm_campaign=Badge_Grade)
[![codecov](https://codecov.io/gh/fbarresi/Sharp7/branch/master/graph/badge.svg)](https://codecov.io/gh/fbarresi/Sharp7)
![Licence](https://img.shields.io/github/license/fbarresi/sharp7.svg)
[![Nuget Version](https://img.shields.io/nuget/v/Sharp7.svg)](https://www.nuget.org/packages/Sharp7/)

Nuget package for Sharp7 - The multi-platform Ethernet S7 PLC communication suite

Sharp7 is a C# port of [Snap7](http://snap7.sourceforge.net) library

For usage and documentation you can visit the [official page](http://snap7.sourceforge.net)
or read the [Wiki](https://github.com/fbarresi/Sharp7/wiki).

# How to install

## Package Manager or dotnet CLI
```
PM> Install-Package Sharp7
```
or
```
> dotnet add package Sharp7
```

# Do you need more power?

Try [Sharp7Reactive](https://github.com/evopro-ag/Sharp7Reactive)

# Get Started

## Supported Targets
- S7 300/400/WinAC CPU (fully supported)
- S7 1200/1500 CPU
- CP (Communication processor - 343/443/IE)

## S7 1200/1500 Notes

An external equipment can access to S71200/1500 CPU using the S7 'base' protocol, only working as an HMI, i.e. only basic data transfer are allowed.

All other PG operations (control/directory/etc..) must follow the extended protocol, not implemented yet.

Particularly **to access a DB in S71500 some additional setting plc-side are needed**.

- Only global DBs can be accessed.

![DB_props](http://snap7.sourceforge.net/snap7_client_file/db_1500.bmp)

- The optimized block access must be turned off.

- The access level must be “full” and the “connection mechanism” must allow GET/PUT.

![DB_sec](http://snap7.sourceforge.net/snap7_client_file/cpu_1500.bmp)