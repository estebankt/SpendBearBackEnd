# Contributing to SpendBear

Thank you for your interest in contributing! SpendBear is an open source project and contributions of all kinds are welcome.

## Ways to Contribute

- Report bugs via [GitHub Issues](../../issues)
- Suggest features or improvements
- Fix bugs or implement features
- Improve documentation
- Write or improve tests

## Getting Started

1. Fork the repository
2. Follow the [Local Development Setup](./README.md#local-development-setup) in the README
3. Create a feature branch from `main`:
   ```bash
   git checkout -b feat/your-feature-name
   ```
4. Make your changes
5. Run the tests:
   ```bash
   dotnet test
   ```
6. Push your branch and open a Pull Request

## Code Conventions

- **Feature folders** over layer folders — organize by feature, not type
- **No MediatR** — direct handler invocation with DI
- **No AutoMapper** — explicit mapping methods
- **Result pattern** for error handling, not exceptions
- **Money as cents** (long) in the database
- Follow existing patterns in the codebase — read a similar module before adding a new one

## Pull Request Guidelines

- Keep PRs focused — one feature or fix per PR
- Add tests for new behavior
- Update documentation if needed
- Describe what the PR does and why in the description

## Reporting Security Issues

Please do **not** open a public issue for security vulnerabilities. Email security@spendbear.com instead.

## License

By contributing, you agree that your contributions will be licensed under the [MIT License](./LICENSE).
