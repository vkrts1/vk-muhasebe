const fs = require('fs');
const path = require('path');

function replaceInDir(dir) {
  const files = fs.readdirSync(dir);
  for (const file of files) {
    const fullPath = path.join(dir, file);
    if (fs.statSync(fullPath).isDirectory()) {
      replaceInDir(fullPath);
    } else if (fullPath.endsWith('.ts') || fullPath.endsWith('.tsx')) {
      let content = fs.readFileSync(fullPath, 'utf8');
      if (content.includes(`'@react-native-async-storage/async-storage'`)) {
        // Find relative path to src/services/storage.ts
        const storagePath = path.resolve(__dirname, 'src/services/storage');
        let relPath = path.relative(path.dirname(fullPath), storagePath).replace(/\\/g, '/');
        if (!relPath.startsWith('.')) relPath = './' + relPath;
        
        content = content.replace(/import AsyncStorage from '@react-native-async-storage\/async-storage';/g, `import AsyncStorage from '${relPath}';`);
        fs.writeFileSync(fullPath, content, 'utf8');
        console.log(`Updated ${fullPath}`);
      }
    }
  }
}

replaceInDir(path.join(__dirname, 'src'));
