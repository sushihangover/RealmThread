
<img style="float: right;" src="https://raw.githubusercontent.com/sushihangover/RealmThread/master/media/SushiHangover.RealmThread.png">

#SushiHangover.RealmThread

An Action/Task Message Pump for running commands on a dedicated Realm thread.

## GitHub Repo

[https://github.com/sushihangover/RealmThread](https://github.com/sushihangover/RealmThread)

##API Documention

[https://sushihangover.github.io/RealmThread/
](https://sushihangover.github.io/RealmThread/)

##Nuget:

<div class="nuget-badge">
<p>
<code>
Install-Package SushiHangover.RealmThread
</code>
</p>
</div>
        
**Nuget.org:** [https://www.nuget.org/packages/sushihangover.realmthread/](https://www.nuget.org/packages/sushihangover.realmthread/)

##Issues?

Post an [Issue](https://github.com/sushihangover/RealmThread/issues) on Github

##Usage:

<div class="code">
TODO
</div>

##Build from Source:

From the cmd line using the amazing [cake](http://cakebuild.net):
<a href="http://cakebuild.net">
<img src="http://cakebuild.net/Content/img/logo.png" alt="cake"/>
</a>
<div class="code">
./build.sh -t Build
</div>

##Build Documention:

API Reference documention is built via the great <a href="http://www.doxygen.org/index.html">
<img src="http://www.stack.nl/~dimitri/doxygen/doxygen.png" alt="doxygen"/>
</a>

<div class="code">
./doxygen Doxygen/realmthread.config
</div>


##Build Nuget Package:

<div class="code">
./build.sh -t Package
</div>

##Publish Nuget:

<pre>
<div class="code">export NUGET_APIKEY={APIKEY}
export GITHUB_TOKEN={TOKEN/PASSWORD}
export GITHUB_USERNAME={EMAILADDRESS}
export NUGET_SOURCE=https://www.nuget.org/api/v2/package
./build.sh -t PublishPackages
</div>
</pre>

<center><sub>Thread Icon within the RealmThread Logo:</sub><br/>
<sub>
Icons made by <a href="http://www.freepik.com" title="Freepik">Freepik</a> from <a href="http://www.flaticon.com" title="Flaticon">www.flaticon.com</a> is licensed by <a href="http://creativecommons.org/licenses/by/3.0/" title="Creative Commons BY 3.0" target="_blank">CC 3.0 BY</a>
</sub></center>

<head>
<style>
.nuget-badge code {
    -moz-border-radius: 5px;
    -webkit-border-radius: 5px;
    background-color: #202020;
    border: 4px solid silver;
    border-radius: 5px;
    box-shadow: 2px 2px 3px #6e6e6e;
    color: #e2e2e2;
    display: block;
    font: 1.0em 'andale mono', 'lucida console', monospace;
    line-height: 1.5em;
    overflow: auto;
    padding: 15px
}
.nuget-badge code::before {
    content: "PM> "
}
.code {
    -moz-border-radius: 5px;
    -webkit-border-radius: 5px;
    background-color: #202020;
    border: 4px solid silver;
    border-radius: 5px;
    box-shadow: 2px 2px 3px #6e6e6e;
    color: #e2e2e2;
    display: block;
    font: 1.0em 'andale mono', 'lucida console', monospace;
    line-height: 1.5em;
    overflow: auto;
    padding: 15px
}

</style>
</head>
