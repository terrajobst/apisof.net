# Investigation

The purpose of this investigation is to find out if a lot of people are using
APIs that exist in ASP.NET Core 2.2 but not in 2.1.

## Finding the data

I have created an ASP.NET Core 2.2 and 2.1 app for .NET Framework 4.7.2. I've
then imported those into API Catalog as fake frameworks (`aspnet2.1` and
`aspnet2.2`).

The tool `GenAspNet22DowngradeBreaks` produces a CSV and binary blog that
contains the set of APIs that exist in `aspnet2.2` but not in `aspnet2.1`
(except for overrides).

## Comparing binaries

The tool `FindAspNetCore22DowngradeBreaks` embeds the blob produced by
`GenAspNet22DowngradeBreaks` as a database of APIs.

It leverages the API Catalog's usage crawler to find all API references in a
given directory. If the API exist in the database, it reports it out.

## CloudMine

To compare the data with CloudMine, we've download the binaries from various
services and ran the tool `FindAspNetCore22DowngradeBreaks`.

Right now we don't have enough binaries to call this result conclusive.

## nuget.org and Upgrade Planner

We should run a similar analysis against the usage data from nuget.org as well
as Upgrade Planner.

