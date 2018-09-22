Copy of the samples used in the original Wave Function Collapse github repo, https://github.com/mxgmn/WaveFunctionCollapse.

These demonstrate a number of different ways that DeBroglie can be used. It also shows how most features of mxgmn's XML format can be translated to DeBroglie's JSON format.

Here's a few notes on translating some XML attributes:

 * `ground` is not supported. I have a similar setting, but it  don't work identically, so I've used 
    border constraints when necessary.
 * `screenshots` not supported.
 * `limit` not supported.
 * `<simple-tiled>` elements not supported at all (yet)

Samples used with permission, many thanks, mxgmn.