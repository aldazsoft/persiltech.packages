const wasmLoader = async (wasmModulePath) => {
    const response = await fetch(wasmModulePath);

    if (!response.ok) {
        throw new Error(`Couldn't fetch ${wasmModulePath}: ${response.status} ${response.statusText}.`);
    }

    const resultObject = await WebAssembly.instantiateStreaming(response);

    return resultObject.instance.exports;
}

export { wasmLoader };
