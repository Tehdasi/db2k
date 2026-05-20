
class Sound {

    buffer = null;

    sources = [];

    constructor() {
    }

    load(b64string) {
        var buff = Uint8Array.fromBase64(b64string);
        audioContext.decodeAudioData(buff.buffer, (buffer) => {
            if (!buffer) {
                console.log(`Sound decoding error: ${this.url}`);
                reject(new Error(`Sound decoding error: ${this.url}`));
                return;
            }

            this.buffer = buffer;
        });
    }

    play(volume = 1, time = 0) {
        if (!this.buffer) return;

        const source = audioContext.createBufferSource();
        source.buffer = this.buffer;
        const insertedAt = this.sources.push(source) - 1;
        source.onended = () => {
            source.stop(0);

            this.sources.splice(insertedAt, 1);
        };
        const gainNode = audioContext.createGain();
        gainNode.gain.value = volume;
        source.connect(gainNode).connect(audioContext.destination);
        source.loop = true;
        source.start(time);
    }

    stop() {
        this.sources.forEach((source) => {
            source.stop(0);
        });

        this.sources = [];
    }
}

const audioContext = new AudioContext();

