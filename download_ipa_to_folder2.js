const fs = require('fs');
const path = require('path');
const { execSync } = require('child_process');

const TOKEN = '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';
// Fetch latest run dynamically
const REPO = 'vkrts1/bawsaq-muhasebe';

const headers = {
  'Authorization': 'Bearer ' + TOKEN,
  'Accept': 'application/vnd.github.v3+json',
  'User-Agent': 'Antigravity'
};

async function sleep(ms) {
  return new Promise(resolve => setTimeout(resolve, ms));
}

async function checkAndDownload() {
  console.log(`[IPA Builder] Searching for latest GitHub Actions Run...`);

  let RUN_ID = null;
  while (!RUN_ID) {
    try {
      const runsRes = await fetch(`https://api.github.com/repos/${REPO}/actions/runs?per_page=1`, { headers });
      const runsData = await runsRes.json();
      if (runsData.workflow_runs && runsData.workflow_runs.length > 0) {
        RUN_ID = runsData.workflow_runs[0].id;
        console.log(`[IPA Builder] Found latest Run ID: ${RUN_ID}`);
      } else {
        console.log(`[IPA Builder] No runs found yet, waiting...`);
        await sleep(10000);
      }
    } catch(e) {
      console.error(`[Error fetching runs] ${e.message}`);
      await sleep(10000);
    }
  }

  console.log(`[IPA Builder] Monitoring GitHub Actions Run ID: ${RUN_ID}...`);

  while (true) {
    try {
      const runRes = await fetch(`https://api.github.com/repos/${REPO}/actions/runs/${RUN_ID}`, { headers });
      const runData = await runRes.json();
      console.log(`[Status] Run status: ${runData.status}, conclusion: ${runData.conclusion}`);

      if (runData.status === 'completed') {
        if (runData.conclusion !== 'success') {
          console.error(`[Error] Build completed with failure: ${runData.conclusion}`);
          process.exit(1);
        }

        console.log('[Success] Build succeeded! Fetching artifacts...');
        const artRes = await fetch(`https://api.github.com/repos/${REPO}/actions/runs/${RUN_ID}/artifacts`, { headers });
        const artData = await artRes.json();
        const artifact = artData.artifacts?.find(a => a.name === 'VK-iOS-IPA');

        if (!artifact) {
          console.error('[Error] VK-iOS-IPA artifact not found in run.');
          process.exit(1);
        }

        console.log(`[Download] Downloading artifact ID: ${artifact.id} (${artifact.size_in_bytes} bytes)...`);
        const dlRes = await fetch(`https://api.github.com/repos/${REPO}/actions/artifacts/${artifact.id}/zip`, { headers });
        
        if (!dlRes.ok) {
          console.error(`[Error] Failed to download artifact: ${dlRes.status} ${dlRes.statusText}`);
          process.exit(1);
        }

        const arrayBuffer = await dlRes.arrayBuffer();
        const zipPath = path.resolve('VK_IPA_Temp2.zip');
        fs.writeFileSync(zipPath, Buffer.from(arrayBuffer));
        console.log(`[Downloaded] Zip saved to ${zipPath}`);

        const tempExtractDir = path.resolve('temp_ipa_extract2');
        if (fs.existsSync(tempExtractDir)) {
          fs.rmSync(tempExtractDir, { recursive: true, force: true });
        }
        fs.mkdirSync(tempExtractDir, { recursive: true });

        console.log('[Extracting] Extracting zip...');
        execSync(`powershell -Command "Expand-Archive -Path '${zipPath}' -DestinationPath '${tempExtractDir}' -Force"`);

        const extractedIpa = path.join(tempExtractDir, 'VK.ipa');
        if (!fs.existsSync(extractedIpa)) {
          console.error('[Error] Extracted folder does not contain VK.ipa');
          process.exit(1);
        }

        const targets = [
          'C:\\Users\\mazik\\OneDrive\\Masaüstü\\Yeni klasör (2)\\VK.ipa',
          'E:\\VK.ipa',
          path.join(process.env.USERPROFILE || 'C:\\Users\\mazik', 'OneDrive', 'Masaüstü', 'VK.ipa')
        ];

        for (const target of targets) {
          try {
            const dir = path.dirname(target);
            if (fs.existsSync(dir)) {
              fs.copyFileSync(extractedIpa, target);
              console.log(`[Copied] Successfully placed fresh VK.ipa at: ${target}`);
            }
          } catch (e) {
            console.warn(`[Warning] Could not copy to ${target}: ${e.message}`);
          }
        }

        // Cleanup
        try {
          fs.unlinkSync(zipPath);
          fs.rmSync(tempExtractDir, { recursive: true, force: true });
        } catch {}

        console.log('[COMPLETE] Fresh VK.ipa is fully delivered!');
        process.exit(0);
      }
    } catch (err) {
      console.warn(`[Poll Exception] ${err.message}`);
    }

    await sleep(30000); // 30 seconds
  }
}

checkAndDownload();
