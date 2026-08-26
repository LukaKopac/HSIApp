import argparse
import json
from pathlib import Path

import joblib
import numpy as np


def read_envi_bil_cube(raw_path: Path) -> np.ndarray:
	header_path = raw_path.with_suffix(".hdr")

	if not raw_path.is_file():
		raise FileNotFoundError(f"Cube file was not found: {raw_path}")

	if not header_path.is_file():
		raise FileNotFoundError(f"Cube header was not found: {header_path}")

	header = {}

	for line in header_path.read_text(encoding="utf-8").splitlines():
		if "=" not in line:
			continue

		key, value = line.split("=", 1)
		header[key.strip().lower()] = value.strip()

	samples = int(header["samples"])
	lines = int(header["lines"])
	bands = int(header["bands"])
	interleave = header["interleave"].lower()
	data_type = int(header["data type"])
	byte_order = int(header.get("byte order", "0"))
	header_offset = int(header.get("header offset", "0"))

	if interleave != "bil":
		raise ValueError(
			f"Unsupported interleave '{interleave}'. Only BIL is supported.")

	if byte_order != 0:
		raise ValueError("Big-endian cubes are not currently supported.")

	if header_offset != 0:
		raise ValueError("Cubes with a header offset are not currently supported.")

	data_types = {
		2: np.dtype("<i2"),  # ENVI signed 16-bit integer
		12: np.dtype("<u2"), # ENVI unsigned 16-bit integer
	}

	if data_type not in data_types:
		raise ValueError(
			f"Unsupported ENVI data type {data_type}. "
			"Supported types are 2 and 12."
		)

	values = np.fromfile(raw_path, dtype=data_types[data_type])

	expected_value_count = lines * samples * bands
	if values.size != expected_value_count:
		raise ValueError(
			f"Cube size does not match its header. "
			f"Expected {expected_value_count} values, found {values.size}."
		)

	# On disk, BIL is [line, band, sample]
	cube = values.reshape(lines, bands, samples)

	# HSIApp's in-memory convention is [line, sample, band]
	cube = np.transpose(cube, (0, 2, 1))

	# Match the existing C# HsiLoader scaling
	return cube.astype(np.float32) / 10000.0

def get_model_path(package_path: Path, manifest: dict) -> Path:
	model_file = manifest["modelFile"]
	model_path = (package_path / model_file).resolve()

	try:
		model_path.relative_to(package_path)
	except ValueError as exception:
		raise ValueError(
			"modelFile must point inside the model package."
		) from exception

	if not model_path.is_file():
		raise FileNotFoundError(
			f"Model file declared in manifest was not found: {model_file}"
		)

	return model_path

def main() -> None:
	parser = argparse.ArgumentParser(
		description="Run a scikit-learn HSIApp model package."
	)
	parser.add_argument("--cube", required=True)
	parser.add_argument("--model-package", required=True)
	parser.add_argument("--output", required=True)
	arguments = parser.parse_args()

	cube_path = Path(arguments.cube).resolve()
	package_path = Path(arguments.model_package).resolve()
	output_path = Path(arguments.output).resolve()

	manifest_path = package_path / "manifest.json"
	if not manifest_path.is_file():
		raise FileNotFoundError(
			f"Model package does not contain manifest.json: {package_path}"
		)

	manifest = json.loads(manifest_path.read_text(encoding="utf-8"))
	model = joblib.load(get_model_path(package_path, manifest))

	if not hasattr(model, "predict"):
		raise TypeError(
			"The loaded joblib object is not a scikit-learn predictor."
		)

	cube = read_envi_bil_cube(cube_path)
	lines, samples, bands = cube.shape

	pixels = cube.reshape(lines * samples, bands)
	prediction = model.predict(pixels)
	prediction_map = prediction.reshape(lines, samples)

	output_path.parent.mkdir(parents=True, exist_ok=True)
	np.save(output_path, prediction_map)

	print(json.dumps({
		"predictionPath": str(output_path),
		"height": lines,
		"width": samples,
		"bands": bands
	}))

if __name__ == "__main__":
	main()