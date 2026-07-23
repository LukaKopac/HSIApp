using HSIApp;
using static System.Net.Mime.MediaTypeNames;

string path = @"D:\PERSONAL\Coding\HSI\Wood species\RawData_eks9_ex-t\SWIR+\scene01\scene01_refl.raw";

HsiMetadata header = HsiLoader.ReadHeader(path);

HsiCube cube = HsiLoader.ReadCube(path, header.Metadata);

float[,] band = cube.GetBand(50);

float[,] normalized = BandRenderer.NormalizeImage(band);

byte[] bytes = BandRenderer.ToByteArray(normalized);
