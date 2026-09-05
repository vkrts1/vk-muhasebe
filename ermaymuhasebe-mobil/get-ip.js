const os = require('os');
const nets = os.networkInterfaces();
for (const name of Object.keys(nets)) {
  for (const net of nets[name]) {
    if (net.family === 'IPv4' && !net.internal && !net.address.startsWith('192.168.56.') && !net.address.startsWith('169.254.')) {
      console.log(net.address);
      process.exit(0);
    }
  }
}
console.log('192.168.3.2');
