# Dockerfile that can be used to develop the samples for Microsoft AI Day 2024.
# It contains:
# * .NET 8 SDK
# * Latest LTS version of Node.js
# * Github CLI

FROM mcr.microsoft.com/dotnet/sdk:8.0

# Install EFCore tools
RUN dotnet tool install --global dotnet-ef

# Install Node Version Manager and Node LTS
RUN mkdir -p /root/.nvm \
  && curl https://raw.githubusercontent.com/creationix/nvm/v0.39.7/install.sh | bash \
  && . /root/.nvm/nvm.sh \
  && nvm install --lts \
  && nvm use --lts

# Install Github CLI
RUN mkdir -p -m 755 /etc/apt/keyrings && wget -qO- https://cli.github.com/packages/githubcli-archive-keyring.gpg | tee /etc/apt/keyrings/githubcli-archive-keyring.gpg > /dev/null \
    && chmod go+r /etc/apt/keyrings/githubcli-archive-keyring.gpg \
    && echo "deb [arch=$(dpkg --print-architecture) signed-by=/etc/apt/keyrings/githubcli-archive-keyring.gpg] https://cli.github.com/packages stable main" | tee /etc/apt/sources.list.d/github-cli.list > /dev/null \
    && apt update \
    && apt install gh -y \
    && apt-get clean \
    && rm -rf /var/lib/apt/lists/*

# Clone sample code
WORKDIR /root
RUN git clone https://github.com/rstropek/microsoft-ai-day.git

# To use GitHub CLI, run `gh auth login`.
# Recommendation: After login, enable GitHub Copilot in the CLI using `gh extension install github/gh-copilot`.
