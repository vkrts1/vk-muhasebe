const fs = require('fs');
const path = require('path');
const { execSync } = require('child_process');

const TOKEN = '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';
const REPO = 'vkrts1/bawsaq-muhasebe';

const headers = {
  'Authorization': 'Bearer ' + TOKEN,
  'Accept': 'application/vnd.github.v3+json',
  'User-Agent': 'Antigravity'
};

async function sleep(ms) {
  return new Promise(resolve => setTimeout(resolve, ms));
}

async function run() {
  console.log('[1/4] Git Add & Commit...');
  try {
    execSync('git add .', { stdio: 'inherit' });
    execSync('git commit -m "fix: cascade deletion for invoices, stock movements and transactions across desktop & mobile"', { stdio: 'inherit' });
  } catch (e) {
    console.log('[Git Commit Notice] ' + (e.message || 'Already committed'));
  }

  console.log('[2/4] Git Push to GitHub main...');
  execSync('git push origin main', { stdio: 'inherit' });

  console.log('[3/4] Waiting for GitHub Actions workflow to start...');
  await sleep(10000);

  let runId = null;
  for (let attempt = 1; attempt <= 12; attempt++) {
    const res = await fetch(`https://api.github.com/repos/${REPO}/actions/runs?per_page=5`, { headers });
    const data = await res.json();
    const latestRun = data.workflow_runs?.[0];
    if (latestRun && latestRun.name === 'Build iOS IPA (ARM64 Sideload)') {
      const createdAt = new Date(latestRun.created_at).getTime();
      if (Date.now() - createdAt < 180000) {
        runId = latestRun.id;
        console.log(`[GitHub Actions] Detected new workflow run ID: ${runId}`);
        console.log(`[GitHub Actions] URL: ${latestRun.html_url}`);
        break;
      }
    }
    console.log(`[Waiting] Workflow run not detected yet (attempt ${attempt}/12)...`);
    await sleep(5000);
  }

  if (!runId) {
    console.error('[Error] Could not detect newly triggered workflow run. Please check GitHub Actions.');
    process.exit(1);
  }

  console.log(`[4/4] Monitoring build progress for Run ID: ${runId}...`);
  const startTime = Date.now();

  while (true) {
    try {
      const runRes = await fetch(`https://api.github.com/repos/${REPO}/actions/runs/${runId}`, { headers });
      const runData = await runRes.json();
      const elapsedMin = ((Date.now() - startTime) / 60000).toFixed(1);
      console.log(`[${elapsedMin} min] Status: ${runData.status}, Conclusion: ${runData.conclusion || 'running'}`);

      if (runData.status === 'completed') {
        if (runData.conclusion !== 'success') {
          console.error(`[Build Failed] GitHub Actions completed with: ${runData.conclusion}`);
          process.exit(1);
        }

        console.log('[Success] Build succeeded! Fetching artifacts...');
        const artRes = await fetch(`https://api.github.com/repos/${REPO}/actions/runs/${runId}/artifacts`, { headers });
        const artData = await artRes.json();
        const artifact = artData.artifacts?.find(a => a.name === 'VK-iOS-IPA');

        if (!artifact) {
          console.error('[Error] Artifact "VK-iOS-IPA" not found in run.');
          process.exit(1);
        }

        console.log(`[Download] Downloading artifact ID: ${artifact.id} (${(artifact.size_in_bytes / 1024 / 1024).toFixed(1)} MB)...`);
        const dlRes = await fetch(`https://api.github.com/repos/${REPO}/actions/artifacts/${artifact.id}/zip`, { headers });

        if (!dlRes.ok) {
          console.error(`[Error] Download failed: ${dlRes.status} ${dlRes.statusText}`);
          process.exit(1);
        }

        const arrayBuffer = await dlRes.arrayBuffer();
        const zipPath = path.resolve('VK_IPA_Download.zip');
        fs.writeFileSync(zipPath, Buffer.from(arrayBuffer));
        console.log(`[Downloaded] Zip saved to ${zipPath}`);

        const tempExtractDir = path.resolve('temp_ipa_extract_new');
        if (fs.existsSync(tempExtractDir)) {
          fs.rmSync(tempExtractDir, { recursive: true, force: true });
        }
        fs.mkdirSync(tempExtractDir, { recursive: true });

        console.log('[Extracting] Extracting zip...');
        execSync(`powershell -Command "Expand-Archive -Path '${zipPath}' -DestinationPath '${tempExtractDir}' -Force"`);

        const extractedIpa = path.join(tempExtractDir, 'VK.ipa');
        if (!fs.existsSync(extractedIpa)) {
          console.error('[Error] Extracted archive does not contain VK.ipa');
          process.exit(1);
        }

        const targets = [
          path.join(process.env.USERPROFILE || 'C:\\Users\\mazik', 'OneDrive', 'Masaüstü', 'VK.ipa'),
          'C:\\Users\\mazik\\OneDrive\\Masaüstü\\Yeni klasör (2)\\VK.ipa',
          path.join(process.env.USERPROFILE || 'C:\\Users\\mazik', 'Downloads', 'VK.ipa'),
          path.resolve('VK.ipa'),
          'E:\\VK.ipa'
        ];

        console.log('[Delivery] Copying fresh VK.ipa to destinations:');
        for (const target of targets) {
          try {
            const dir = path.dirname(target);
            if (fs.existsSync(dir)) {
              fs.copyFileSync(extractedIpa, target);
              console.log(` -> [OK] ${target}`);
            }
          } catch (e) {
            console.warn(` -> [Skipped] ${target}: ${e.message}`);
          }
        }

        // Cleanup
        try {
          fs.unlinkSync(zipPath);
          fs.rmSync(tempExtractDir, { recursive: true, force: true });
        } catch {}

        console.log(`\n=====================================================================`);
        console.log(` [TEBRİKLER] YENİ GÜNCEL VK.ipa BAŞARIYLA HAZIRLANDI!`);
        console.log(` Dosya Masaüstüne kopyalandı.`);
        console.log(`=====================================================================\n`);
        process.exit(0);
      }
    } catch (err) {
      console.warn(`[Poll Warning] ${err.message}`);
    }

    await sleep(20000); // Poll every 20 seconds
  }
}

run().catch(err => {
  console.error('[Fatal Error]', err);
  process.exit(1);
});
