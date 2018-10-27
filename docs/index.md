---
title: DeBroglie Documentation
documentType: index
_disableFooter: true
_disableBreadcrumb: true
_disableToc: true
_gitContribute: false
---
<div class="container">

  <div class="jumbotron">
    <h1 class="display-4">DeBroglie</h1>
    <p class="lead">Generate tile based maps using Wave Function Collapse</p>
    <small class="text-muted"><a class="github-link" href="https://github.com/BorisTheBrave/DeBroglie">View in Github</a><small>
    <hr class="my-4">
    <p>Stuffed with loads of features to provide precise control over generation.</p>
    <p class="lead">
      <a class="btn btn-primary btl-lg" href="articles/index.md" role="button">Getting Started</a>
      <a class="btn btn-primary btl-lg" href="https://github.com/BorisTheBrave/DeBroglie/releases" role="button">Download Latest</a>
      <a class="btn btn-primary btl-lg" href="articles/release_notes.md" role="button">Update Log</a>
    </p>
  </div>

  <div class="row">
    <div class="col-md-8 col-md-offset-2 text-center">
      <section>
        <h2>C# Library and Command line Program</h2>
        <p class="lead">Control generation directly in C# within Unity or .NET Core.</p>
        <p>Generate maps from JSON configuration files using the executable (Windows only)</p> 
      </section>
    </div>
  </div>

  
  <div class="row">
    <div class="col-md-8 col-md-offset-2 text-center">
      <section>
        <h2>Features</h2>
        <h3>Generate tile maps using the WFC algorithm</h3>
        <a href="https://github.com/BorisTheBrave/DeBroglie/blob/master/samples/grass/map.json"><video src="images/rotation.webm" autoplay loop></video></a>
        <h3><a href="articles/features.md#topology">2d, 3d and hexagonal generation</a></h3>
        <a href="images/columns_out.png"><img src="images/columns_out.png"/></a>
        <a href="images/hexmini_out.png"><img src="images/hexmini_out.png"/></a>
        <h3>Constraint generation to only connected paths</h3>
        <img src="images/pathway_overlapping_path.png">
        <h3>Backtracking support for tough-to-generate setups</h3>
      </section>
    </div>
  </div>
</div>