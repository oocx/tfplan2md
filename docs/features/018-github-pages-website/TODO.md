# Website TODO

## Deployment

The website is deployed to GitHub Pages using the workflow defined in `.github/workflows/deploy-website.yml`.

**To deploy the website:**
1. Go to the Actions tab in the GitHub repository
2. Select "Deploy Website" workflow
3. Click "Run workflow" and select the branch to deploy
4. The website will be deployed to GitHub Pages

**Note:** The GitHub Pages site visibility (public/private) is configured in the repository settings, not in the workflow.

## High Priority
- [ ] Replace mockup examples on examples page with real tfplan2md output
  - Use examples from `artifacts/comprehensive-demo.md` or `examples/` directory
  - Ensure both rendered and source views show actual tfplan2md markdown
  - Maintain syntax highlighting and Azure DevOps styling

## Future Enhancements
- [ ] Add more real-world examples for different resource types
- [ ] Create interactive demos showing before/after comparisons
- [ ] Add user testimonials or case studies (if available)
