#!/usr/bin/env node
/**
 * Post or update a PR comment with benchmark results
 * Usage: node post-pr-comment.js <markdownFile> <prNumber> <repoOwner> <repoName> <githubToken> <runId> <commitSha>
 *
 * This script requires GITHUB_TOKEN environment variable or passed as argument
 */

const fs = require("fs");
const https = require("https");

async function makeGitHubRequest(method, path, token, data = null) {
  return new Promise((resolve, reject) => {
    const options = {
      hostname: "api.github.com",
      port: 443,
      path: path,
      method: method,
      headers: {
        "User-Agent": "benchmark-comment-script",
        Authorization: `token ${token}`,
        Accept: "application/vnd.github.v3+json",
        "Content-Type": "application/json",
      },
    };

    const req = https.request(options, (res) => {
      let body = "";
      res.on("data", (chunk) => (body += chunk));
      res.on("end", () => {
        if (res.statusCode >= 200 && res.statusCode < 300) {
          resolve(JSON.parse(body || "{}"));
        } else {
          reject(new Error(`GitHub API error: ${res.statusCode} - ${body}`));
        }
      });
    });

    req.on("error", reject);

    if (data) {
      req.write(JSON.stringify(data));
    }

    req.end();
  });
}

async function listComments(owner, repo, prNumber, token) {
  const path = `/repos/${owner}/${repo}/issues/${prNumber}/comments`;
  return await makeGitHubRequest("GET", path, token);
}

async function createComment(owner, repo, prNumber, token, body) {
  const path = `/repos/${owner}/${repo}/issues/${prNumber}/comments`;
  return await makeGitHubRequest("POST", path, token, { body });
}

async function updateComment(owner, repo, commentId, token, body) {
  const path = `/repos/${owner}/${repo}/issues/comments/${commentId}`;
  return await makeGitHubRequest("PATCH", path, token, { body });
}

async function main() {
  const args = process.argv.slice(2);

  if (args.length < 4) {
    console.error(
      "Usage: node post-pr-comment.js <markdownFile> <prNumber> <repoOwner> <repoName> [githubToken] [runId] [commitSha]",
    );
    console.error("");
    console.error("Environment variables:");
    console.error(
      "  GITHUB_TOKEN - GitHub API token (can be passed as 5th arg)",
    );
    console.error("");
    console.error("Example:");
    console.error(
      "  node post-pr-comment.js /tmp/comment.md 123 owner repo $GITHUB_TOKEN 456 abc123",
    );
    process.exit(1);
  }

  const [
    markdownFile,
    prNumber,
    repoOwner,
    repoName,
    githubToken,
    runId,
    commitSha,
  ] = args;
  const token = githubToken || process.env.GITHUB_TOKEN;

  if (!token) {
    console.error(
      "❌ ERROR: GitHub token required (pass as argument or set GITHUB_TOKEN env var)",
    );
    process.exit(1);
  }

  console.error("=== PR Comment Poster ===");
  console.error(`Markdown file: ${markdownFile}`);
  console.error(`PR Number: ${prNumber}`);
  console.error(`Repository: ${repoOwner}/${repoName}`);
  console.error("");

  // Read markdown content
  let comment = "";
  try {
    comment = fs.readFileSync(markdownFile, "utf8");
    console.error(`✓ Loaded markdown content (${comment.length} bytes)`);
  } catch (error) {
    console.error(`⚠️ Could not read markdown file: ${error.message}`);
    comment = "## 📊 Benchmark Results\n\n";
    comment += `⚠️ Could not read benchmark results: ${error.message}\n\n`;
    comment += "View artifacts for detailed results.\n";
  }

  // Add metadata and marker
  comment += "\n---\n";
  if (runId && commitSha) {
    comment += `Run ID: ${runId} | Commit: ${commitSha.substring(0, 7)}\n`;
  }
  comment += "<!-- benchmark-results-comment -->";

  try {
    // List all comments on the PR
    console.error("Fetching existing PR comments...");
    const comments = await listComments(repoOwner, repoName, prNumber, token);
    console.error(`Found ${comments.length} total comments`);

    // Find existing benchmark comment
    const existingComment = comments.find(
      (c) => c.body && c.body.includes("<!-- benchmark-results-comment -->"),
    );

    if (existingComment) {
      console.error(
        `Found existing comment (ID: ${existingComment.id}), updating...`,
      );
      await updateComment(
        repoOwner,
        repoName,
        existingComment.id,
        token,
        comment,
      );
      console.error("✅ Successfully updated existing comment");
    } else {
      console.error("No existing comment found, creating new one...");
      const result = await createComment(
        repoOwner,
        repoName,
        prNumber,
        token,
        comment,
      );
      console.error(`✅ Successfully created new comment (ID: ${result.id})`);
    }
  } catch (error) {
    console.error("❌ ERROR:", error.message);
    process.exit(1);
  }
}

// Run if called directly
if (require.main === module) {
  main().catch((error) => {
    console.error("❌ Fatal error:", error.message);
    console.error(error.stack);
    process.exit(1);
  });
}

module.exports = { listComments, createComment, updateComment };
