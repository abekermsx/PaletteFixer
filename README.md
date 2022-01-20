# PaletteFixer
Simple tool for automatic reordering of palettes of multiple .GE5 images

Usage: PaletteFixer [-rgb] [-data] [-fix] image1.ge5 image2.ge5 ...
   -rgb  : Output palette of all files in RGB format
   -data : Output palette of all files as data statement
   -fix  : Reorder colors in image2.ge5, image3.ge5, ... to match palette of image1 and save to image<x>-new.ge5

If -fix is specified, -rgb and -data output the resulting palettes
