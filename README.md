# Introduction 
This application was written to read comma-separated capacitance measurements for a 16 electrode sensor in pF over a Serial UART connection and display the computed tomograph as well as the normalized capacitance measurements.  
  
To use, the solution can be built using Microsoft Visual Studio. A sample sensitivity matrix is included in the files for a circular sensor. 
## Serial Data Format
The application expects to read a string of float values over a serial connection, which take the form:  
``
"r000001.004,1.0500,2.86,...,{n=120}"
``  
The application uses the `` 'r' `` character at the beginning of the string to begin parsing the data, so it must be added to the beginning of the data string being sent to the application. The the included sensitivity map was calculated for a 16 electrode sensor, which has (16 x 15)/2=120 unique electrode pairings. Therefore, the serial data must contain 120 comma-separated values. To help prevent data truncation, adding a few extra zeroes to the 1st entry is a good practice. The units of the float values are assumed to be in picoFarads, but this depends on the sensitivity matrix.
## Sensitivity Matrix
The application uses a MxN matrix of values as the sensitivity map, where M is the square of the image dimension in pixels (50x50=2500) and N is the number of unique electrode pairings (120 for a 16 electrode sensor). This is stored as a .txt file and has the format:  
``
{
  {S12-1,S12-2,...,S12-2500},{S13-1,S13-2,...,S13-2500},...{S1516-1,S1516-2,...,S1516-2500}
}
``
## Image Reconstruction
Two reconstruction algorithms have been implemented in this application: linear back projection (LBP) and Tikhonov Regularization Back Projection (TRBP). Neither is more computationally taxing than the other. Before reading any capacitance values, the application loads the sensitivity matrix from the stored text file as an array of doubles. Once a list of capacitance values have been parsed from a Serial connection, the image is reconstructed using one of the selected algorithms. The resulting ``[120].[120,2500]=[2500]`` length list is then resized into a 50 x 50 array and the values mapped onto a bitmap image. 
