var canvas = document.getElementById("myCanvas");
/** @type {CanvasRenderingContext2D} */
var ctx = canvas.getContext("2d");
ctx.imageSmoothingEnabled = false;
var imageObj = new Image();

imageObj.onload = function() {
//context.drawImage(imageObj, 69, 50);
};
imageObj.src = 'images/overworld_tileset_grass.png';


var t = 0;
function step()
{
    var d = (t / 1000.0) % 1;
    theta = (d < 0.25 ? d : d < 0.5 ? 0.25 : d < 0.75 ? 0.75 - d : 0.0) * Math.PI * 2;
    ctx.clearRect(0, 0, canvas.width, canvas.height);
    var data = [
        [7, 20, 1],
        [1, 42, 7]
    ]
    var size = 80;

    for(var x=0;x<3;x++)
    {
        for(var y=0;y<2;y++)
        {
            ctx.fillStyle= ['#FF0000', '#00FF00', '#0000FF'][(x+y) % 3];
            var c = Math.cos(theta);
            var s = Math.sin(theta);
            var cx = (x - 1) * c + (y - 0.5) * s + Math.sqrt(2);
            var cy = (x - 1) * s - (y - 0.5) * c + Math.sqrt(2);
            var gid = data[1 - y][x];
            var ix = Math.floor((gid - 1) % 12);
            var iy = Math.floor((gid - 1) / 12);
            ctx.drawImage(imageObj, ix * 16, iy * 16, 16, 16, Math.round(cx*size), Math.round(cy*size), size, size);
        }
    }
    requestAnimationFrame(step);
    t++;
}

step();