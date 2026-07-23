// Development playground for testing HSI functionality.
// This project is not part of the main application.

using HSIApp;
using static System.Net.Mime.MediaTypeNames;

string path = @"C:\Path\To\TestData\sample.raw";

HsiMetadata header = HsiLoader.ReadHeader(path);

HsiCube cube = HsiLoader.ReadCube(path, header.Metadata);

float[,] band = cube.GetBand(50);

float[,] normalized = BandRenderer.NormalizeImage(band);

byte[] bytes = BandRenderer.ToByteArray(normalized);
