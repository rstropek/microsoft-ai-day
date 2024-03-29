import readline from 'readline';

export function readLine(prompt: string): Promise<string> {
    return new Promise((resolve) => {
        const rl = readline.createInterface({
            input: process.stdin,
            output: process.stdout
        });

        rl.question(prompt, (answer: string) => {
            rl.close();
            resolve(answer);
        });
    });
}
