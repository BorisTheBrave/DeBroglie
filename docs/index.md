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
        <p>Generate maps from JSON configuration files using the executable.</p> 
      </section>
    </div>
  </div>

  <div class="row">
    <div class="col-md-8 col-md-offset-2 text-center">
      <style>
      .carousel-indicators li {
          border-color: #BBBBBB;
          background-color: #444444;
      }
      .carousel-indicators .active {
          background-color: #999999;
      }
      .item {
          position: relative;
          height:100%;
      }
      .carousel-inner img {
          position: absolute;
          top: 50%;
          left: 50%;
          transform: translateY(-50%) translateX(-50%);
      }
      </style>
      <div id="carousel" class="carousel slide" data-ride="carousel" data-interval="8000">
        <!-- Indicators -->
        <ol class="carousel-indicators">
          <li data-target="#carousel" data-slide-to="0" class="active"></li>
          <li data-target="#carousel" data-slide-to="1"></li>
          <li data-target="#carousel" data-slide-to="2"></li>
          <li data-target="#carousel" data-slide-to="3"></li>
          <li data-target="#carousel" data-slide-to="4"></li>
          <li data-target="#carousel" data-slide-to="5"></li>
        </ol>
        <!-- Wrapper for slides -->
        <div class="carousel-inner" role="listbox" style="width:100%; height: 320px !important;">
          <div class="item active">
            <a href="https://github.com/BorisTheBrave/DeBroglie/blob/master/samples/platformer/platformer.json">
            <video src="images/platformer.webm" autoplay loop muted width="640" height="320"
                style="background-color: #55b4ff">
            </video>
            </a>
          </div>
          <div class="item">
            <a href="https://github.com/BorisTheBrave/DeBroglie/blob/master/samples/castle/castle.json">
            <video src="images/castle_fixed.webm" autoplay loop muted>
            </a>
          </div>
          <div class="item">
            <a href="https://github.com/BorisTheBrave/DeBroglie/blob/master/samples/grass/map.json"><video src="images/rotation.webm" autoplay loop></video></a>
          </div>
          <div class="item">
            <a href="https://github.com/BorisTheBrave/DeBroglie/blob/master/samples/docs/columns.json"><img src="images/columns_out.png"/></a>
          </div>
          <div class="item">
            <a href="https://github.com/BorisTheBrave/DeBroglie/blob/master/samples/docs/hexmini.json"><img src="images/hexmini_out.png"/></a>
          </div>
          <div class="item">
            <a href="https://github.com/BorisTheBrave/DeBroglie/blob/master/samples/mxgmn/circles.json">
            <video src="images/circles.webm" autoplay loop muted>
            </video>
            </a>
          </div>
        </div>
        <!-- Controls -->
        <a class="left carousel-control" data-target="#carousel" role="button" data-slide="prev">
          <span class="glyphicon glyphicon-chevron-left" aria-hidden="true"></span>
          <span class="sr-only">Previous</span>
        </a>
        <a class="right carousel-control" data-target="#carousel" role="button" data-slide="next">
          <span class="glyphicon glyphicon-chevron-right" aria-hidden="true"></span>
          <span class="sr-only">Next</span>
        </a>
      </div>
    </div>
  </div>

  <div class="row">
    <div class="col-md-8 col-md-offset-2 text-center">
      <section>
        <h2>Features</h2>
        <h3>Generate tile maps using the WFC algorithm</h3>
        <h3><a href="articles/features.md#topology">2d, 3d and hexagonal generation</a></h3>
        <h3><a href="articles/constraints.md#path">Constraint generation to only connected paths</a></h3>
        <h3><a href="articles/features.md#backtracking">Backtracking support for tough-to-generate setups</a></h3>
      </section>
    </div>
  </div>
</div>