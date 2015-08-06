# NuGetProxy
NuGet Proxy Server

NuGet Server has bug, query Packages(Id=,Version=) fails to get latest version, so I found out an alternative way to query and return same package through OData query, surprisingly, NuGet's package restore does not use OData and fails to restore package. But this server can act as proxy.

Steps to use

1. Host this on some live web server with SSL installed.
2. Modify NuGet.targets file and add custom feed as <PackageSource Include="https://yourserver.com/api/v2/" />

How does this work?

This project is nothing but simple URL Rewriter and a reverse proxy, it rewrites URL `Package(Id='',Version='')` to equivalent OData query as shown below.

    `Packages()?$filter=(Id eq '')and(Version eq '')`
    



