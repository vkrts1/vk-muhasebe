const fs = require('fs');
const path = require('path');

const screens = [
  'CarilerScreen.tsx',
  'FaturalarScreen.tsx',
  'SiparislerScreen.tsx',
  'TekliflerScreen.tsx',
  'FinansScreen.tsx'
];

const basePath = path.join(__dirname, 'src', 'screens');

screens.forEach(screen => {
  const filePath = path.join(basePath, screen);
  if (!fs.existsSync(filePath)) {
    console.log(`File not found: ${filePath}`);
    return;
  }

  let content = fs.readFileSync(filePath, 'utf8');
  
  // Add import if not present
  if (!content.includes(`import { FlashList } from '@shopify/flash-list'`)) {
    content = content.replace(/(import .* from 'react-native';)/, `$1\nimport { FlashList } from '@shopify/flash-list';`);
  }

  // Replace <FlatList and </FlatList>
  content = content.replace(/<FlatList/g, '<FlashList estimatedItemSize={100}');
  content = content.replace(/<\/FlatList>/g, '</FlashList>');

  fs.writeFileSync(filePath, content, 'utf8');
  console.log(`Updated ${screen}`);
});
