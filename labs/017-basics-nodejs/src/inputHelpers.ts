import readline from 'readline';

export function readLine(prompt: string): Promise<string> {
  return new Promise((resolve) => {
    const rl = readline.createInterface({
      input: process.stdin,
      output: process.stdout,
    });

    rl.question(prompt, (answer) => {
      rl.close();
      resolve(answer);
    });
  });
}

export function readKey(): Promise<void> {
  return new Promise((resolve) => {
    process.stdin.setRawMode(true);
    process.stdin.setEncoding('utf-8');

    const onKeyPress = (key: any) => {
      process.stdin.removeListener('data', onKeyPress);
      process.stdin.setRawMode(false);
      resolve(key);
    };

    process.stdin.on('data', onKeyPress);
  });
}
