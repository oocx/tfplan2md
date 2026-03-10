const githubActions = {
  id: "github-actions",
  tabLabel: "GitHub Actions",
  sectionTitle: "GitHub Actions Workflow",
  description: "Add this job to your workflow to automatically comment on PRs with Terraform plans.",
  fileName: ".github/workflows/terraform.yml",
  code: String.raw`name: Terraform Plan

on:
  pull_request:
    paths:
      - 'terraform/**'

jobs:
  plan:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4

      - name: Setup Terraform
        uses: hashicorp/setup-terraform@v3
        with:
          terraform_version: 1.9.0

      - name: Terraform Init
        run: terraform init
        working-directory: ./terraform

      - name: Terraform Plan
        run: terraform plan -out=plan.tfplan
        working-directory: ./terraform

      - name: Convert plan to JSON
        run: terraform show -json plan.tfplan > plan.json
        working-directory: ./terraform

      - name: Generate markdown report
        run: |
          docker run -v $(pwd):/data oocx/tfplan2md \
            /data/plan.json --output /data/plan.md
        working-directory: ./terraform

      - name: Post PR comment
        uses: actions/github-script@v7
        with:
          script: |
            const fs = require('fs');
            const plan = fs.readFileSync('terraform/plan.md', 'utf8');

            github.rest.issues.createComment({
              issue_number: context.issue.number,
              owner: context.repo.owner,
              repo: context.repo.repo,
              body: plan
            });`
};

const azurePipelines = {
  id: "azure-pipelines",
  tabLabel: "Azure Pipelines",
  sectionTitle: "Azure Pipelines Configuration",
  description: "Use this pipeline template to post Terraform plans to Azure DevOps pull requests.",
  fileName: "azure-pipelines.yml",
  code: String.raw`trigger: none

pr:
  branches:
    include:
      - main
  paths:
    include:
      - terraform/*

pool:
  vmImage: 'ubuntu-latest'

steps:
  - task: TerraformInstaller@1
    inputs:
      terraformVersion: '1.9.0'

  - script: terraform init
    displayName: 'Terraform Init'
    workingDirectory: terraform

  - script: terraform plan -out=plan.tfplan
    displayName: 'Terraform Plan'
    workingDirectory: terraform

  - script: terraform show -json plan.tfplan > plan.json
    displayName: 'Convert to JSON'
    workingDirectory: terraform

  - script: |
      docker run -v $(pwd):/data oocx/tfplan2md \
        /data/plan.json --output /data/plan.md
    displayName: 'Generate markdown report'
    workingDirectory: terraform

  - bash: |
      MARKDOWN=$(cat terraform/plan.md)
      URL="$(System.TeamFoundationCollectionUri)$(System.TeamProject)/_apis/git/repositories/$(Build.Repository.ID)/pullRequests/$(System.PullRequest.PullRequestId)/threads?api-version=7.0"

      BODY=$(jq -n \
        --arg content "$MARKDOWN" \
        '{comments: [{content: $content, commentType: 1}], status: 1}')

      curl -X POST "$URL" \
        -H "Content-Type: application/json" \
        -H "Authorization: Bearer $(System.AccessToken)" \
        -d "$BODY"
    displayName: 'Post PR comment'`
};

const gitlabCi = {
  id: "gitlab-ci",
  tabLabel: "GitLab CI",
  sectionTitle: "GitLab CI Configuration",
  description: "Configure GitLab CI to post Terraform plans as merge request comments.",
  fileName: ".gitlab-ci.yml",
  note: {
    label: "Note",
    text: "Create a GitLab access token with api scope and add it as GITLAB_TOKEN in CI/CD variables."
  },
  code: String.raw`terraform-plan:
  stage: plan
  image: hashicorp/terraform:1.9
  only:
    - merge_requests
  script:
    - cd terraform
    - terraform init
    - terraform plan -out=plan.tfplan
    - terraform show -json plan.tfplan > plan.json

    - |
      docker run -v $(pwd):/data oocx/tfplan2md \
        /data/plan.json --output /data/plan.md

    - |
      MARKDOWN=$(cat plan.md)
      curl --request POST \
        --header "PRIVATE-TOKEN: $GITLAB_TOKEN" \
        --header "Content-Type: application/json" \
        --data "{\"body\": \"$MARKDOWN\"}" \
        "$CI_API_V4_URL/projects/$CI_PROJECT_ID/merge_requests/$CI_MERGE_REQUEST_IID/notes"`
};

const securityTools = {
  id: "security-tools",
  tabLabel: "Security Tools",
  sectionTitle: "Security Tools Workflow",
  description: "Combine infrastructure changes with findings from multiple SARIF-compatible scanners.",
  fileName: "security-tools.yml",
  code: String.raw`- name: Run security scans
  run: |
    # Run Checkov
    checkov -d terraform --framework terraform \
      --output sarif --output-file-path . --compact

    # Run TfLint
    cd terraform && tflint --format sarif > ../tflint.sarif && cd ..

    # Run Trivy
    trivy config terraform --format sarif --output trivy.sarif

- name: Generate unified report
  run: |
    terraform show -json plan.tfplan | \
      docker run -i -v $(pwd):/data oocx/tfplan2md \
        --code-analysis-results "/data/**/*.sarif" > plan.md

- name: Post PR comment
  uses: actions/github-script@v7
  with:
    script: |
      const fs = require('fs');
      const plan = fs.readFileSync('plan.md', 'utf8');

      github.rest.issues.createComment({
        issue_number: context.issue.number,
        owner: context.repo.owner,
        repo: context.repo.repo,
        body: plan
      });`
};

module.exports = {
  examples: {
    githubActions,
    azurePipelines,
    gitlabCi,
    securityTools
  },
  homepageTabs: [githubActions, azurePipelines, securityTools],
  gettingStartedTabs: [githubActions, azurePipelines, gitlabCi]
};