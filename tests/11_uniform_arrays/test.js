//#glsl "test.glsl"

function update(frame, ut) {
    u_floats.set(new Float32Array([0.1, 0.2, 0.3, 0.4]));
    u_ints.set(new Int32Array([1, 2]));
    u_uints.set(new Uint32Array([10, 20, 30]));
    u_vecs.set(new Float32Array([1.0, 0.0, 0.0, 0.0, 1.0, 0.0])); // two vec3s
    return 0;
}
