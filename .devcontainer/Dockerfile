FROM unityci/editor:ubuntu-2022.3.22f1-base-3

# X11/GUI, Japanese fonts, and dev tools
RUN apt-get update && apt-get install -y --no-install-recommends \
    libx11-6 \
    libxcursor1 \
    libxrandr2 \
    libglu1-mesa \
    mesa-utils \
    x11-utils \
    fonts-noto-cjk \
    git \
    curl \
    jq \
    && rm -rf /var/lib/apt/lists/*

# vrc-get (VPM CLI tool)
RUN ARCH=$(dpkg --print-architecture) && \
    case "$ARCH" in \
      amd64) VRC_ARCH="x86_64-unknown-linux-musl" ;; \
      arm64) VRC_ARCH="aarch64-unknown-linux-musl" ;; \
      *) echo "Unsupported architecture: $ARCH" && exit 1 ;; \
    esac && \
    VRC_GET_VERSION=$(curl -fsSL https://api.github.com/repos/vrc-get/vrc-get/releases/latest | jq -r '.tag_name') && \
    curl -fsSL "https://github.com/vrc-get/vrc-get/releases/download/${VRC_GET_VERSION}/${VRC_ARCH}-vrc-get" -o /usr/local/bin/vrc-get && \
    chmod +x /usr/local/bin/vrc-get
