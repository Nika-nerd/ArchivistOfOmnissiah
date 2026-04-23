
import React from 'react';
import { ReactP5Wrapper, type Sketch } from 'react-p5-wrapper';

const sketch: Sketch = (p5) => {
    const drops: number[] = [];
    const fontSize = 16;

    p5.setup = () => {
        p5.createCanvas(p5.windowWidth, p5.windowHeight);
        p5.frameRate(30);
        const columns = p5.width / fontSize;
        for (let i = 0; i < columns; i++) {
            drops[i] = p5.random(-500, 0);
        }
    };

    p5.draw = () => {

        p5.background(5, 0, 0, 20);

        p5.fill(255, 0, 0, 90);
        p5.textSize(fontSize);
        p5.textFont('Courier New');

        for (let i = 0; i < drops.length; i++) {
            const text = p5.random() > 0.5 ? '1' : '0';
            const x = i * fontSize;
            const y = drops[i] * fontSize;

            p5.text(text, x, y);

            if (y > p5.height && p5.random() > 0.975) {
                drops[i] = 0;
            }

            drops[i]++;
        }
    };

    p5.windowResized = () => {
        p5.resizeCanvas(p5.windowWidth, p5.windowHeight);

        const columns = p5.width / fontSize;
        if(drops.length < columns) {
            for(let i = drops.length; i < columns; i++) {
                drops[i] = p5.random(-500, 0);
            }
        }
    };
};

export const BinaryBackground: React.FC = () => {
    return <ReactP5Wrapper sketch={sketch} />;
};