# Windows Margin Manager Pro 🚀

**The Ultimate Intelligent Window Management Powerhouse**

A sophisticated C# utility that revolutionizes window management with AI-powered positioning, multi-monitor mastery, and comprehensive automation features. Transform your desktop workflow with intelligent window orchestration.

## 🌟 Core Features

### 🎯 **Intelligent Window Management**
- **Smart Hotkey System**: Multiple configurable hotkeys (Ctrl+Alt+M, Ctrl+Alt+1-9, custom combinations)
- **AI-Powered Positioning**: Machine learning algorithms analyze usage patterns for optimal window placement
- **Dynamic Margin Calculation**: Adaptive margins based on screen resolution, window content, and user behavior
- **Multi-Monitor Mastery**: Full support for unlimited monitors with per-monitor configurations
- **Window Intelligence**: Automatic detection of window types, applications, and optimal sizing strategies

### 🎨 **Visual Excellence**
- **Smooth Animations**: Buttery-smooth window transitions with customizable easing curves
- **Visual Feedback**: Real-time preview overlays showing target positions before applying
- **Theme System**: Dark/Light themes with custom color schemes and transparency effects
- **Notification System**: Elegant toast notifications with progress indicators
- **Tray Icon Animations**: Dynamic system tray icon reflecting current state

### 🧠 **Advanced Configuration**
- **Profile Management**: Unlimited named profiles for different workflows (Work, Gaming, Design, etc.)
- **Application-Specific Rules**: Custom behaviors per application with regex pattern matching
- **Window Filtering**: Advanced filters by title, class, process, size, position, and custom criteria
- **Exclusion Lists**: Smart exclusion of system windows, dialogs, and specified applications
- **Conditional Logic**: If-then rules based on time, active application, monitor configuration, etc.

### ⚡ **Automation & Scheduling**
- **Time-Based Automation**: Schedule window arrangements at specific times or intervals
- **Event-Driven Actions**: Trigger arrangements on application launch, monitor changes, or system events
- **Workspace Switching**: Instant switching between predefined window layouts
- **Session Management**: Save and restore complete desktop sessions
- **Auto-Save States**: Automatic backup of window positions before changes

### 🔧 **Power User Features**
- **Command Line Interface**: Full CLI support for scripting and automation
- **API Integration**: REST API for third-party integrations and remote control
- **Plugin System**: Extensible architecture for custom functionality
- **Macro Recording**: Record and replay complex window manipulation sequences
- **Batch Operations**: Apply operations to multiple windows simultaneously

### 📊 **Analytics & Insights**
- **Usage Analytics**: Track window management patterns and productivity metrics
- **Performance Monitoring**: Real-time performance metrics and optimization suggestions
- **Heatmaps**: Visual representation of most-used screen areas
- **Productivity Reports**: Daily/weekly reports on window management efficiency

## 🎮 **Hotkey System**

| Hotkey | Function | Customizable |
|--------|----------|--------------|
| `Ctrl+Alt+M` | Apply current profile margins | ✅ |
| `Ctrl+Alt+1-9` | Quick profile switching | ✅ |
| `Ctrl+Alt+R` | Restore original positions | ✅ |
| `Ctrl+Alt+S` | Save current layout as profile | ✅ |
| `Ctrl+Alt+A` | Auto-arrange windows intelligently | ✅ |
| `Ctrl+Alt+F` | Toggle fullscreen mode | ✅ |
| `Ctrl+Alt+T` | Tile windows automatically | ✅ |
| `Win+Shift+M` | Multi-monitor window distribution | ✅ |

## 🖥️ **Multi-Monitor Excellence**

- **Per-Monitor Profiles**: Different margin settings for each display
- **Monitor Detection**: Automatic detection of monitor changes and reconfiguration
- **Bezel Compensation**: Smart handling of monitor bezels and gaps
- **Resolution Scaling**: DPI-aware calculations for mixed-resolution setups
- **Virtual Desktop Support**: Windows 11 virtual desktop integration
- **Ultrawide Support**: Specialized handling for ultrawide and curved monitors

## ⚙️ **Configuration Options**

### Margin Types
- **Fixed Pixels**: Precise pixel-based margins (e.g., 50px from each edge)
- **Percentage**: Relative percentage margins (e.g., 5% from each edge)
- **Dynamic**: AI-calculated margins based on content and usage patterns
- **Adaptive**: Margins that adjust based on window count and screen real estate
- **Golden Ratio**: Aesthetically pleasing proportions using mathematical ratios

### Window Behaviors
- **Smooth Animations**: Configurable animation duration and easing
- **Collision Detection**: Prevent window overlaps with intelligent positioning
- **Snap Zones**: Magnetic snap areas for precise positioning
- **Aspect Ratio Preservation**: Maintain window proportions during resize
- **Minimum Size Enforcement**: Respect application minimum window sizes

### Advanced Filters
- **Application Whitelist/Blacklist**: Include or exclude specific applications
- **Window Title Patterns**: Regex-based title matching
- **Window State Filters**: Target only maximized, minimized, or normal windows
- **Size Thresholds**: Apply rules based on window dimensions
- **Monitor-Specific Rules**: Different behaviors per monitor

## 🚀 **Installation & Setup**

### Requirements
- **OS**: Windows 10 (1903+) or Windows 11
- **Runtime**: .NET 8.0 or later (LTS)
- **Memory**: 50MB RAM (minimal footprint)
- **Permissions**: Standard user (no admin required)

### Quick Start
```bash
# Clone and build
git clone https://github.com/VpkDevs/windows-margin-manager.git
cd windows-margin-manager
dotnet build --configuration Release

# Run
dotnet run --project WindowsMarginManager
```

### Advanced Installation
```bash
# Create standalone executable
dotnet publish -c Release -r win-x64 --self-contained true

# Install as Windows service (optional)
sc create "WindowsMarginManager" binPath="C:\Path\To\WindowsMarginManager.exe --service"
```

## 📖 **Usage Examples**

### Basic Usage
1. **Launch**: Run the application (auto-starts in system tray)
2. **Configure**: Right-click tray icon → Settings
3. **Apply**: Press `Ctrl+Alt+M` to apply margins
4. **Restore**: Press `Ctrl+Alt+R` to restore original positions

### Advanced Workflows
```csharp
// Example: Create a "Coding" profile
Profile codingProfile = new Profile("Coding")
{
    LeftMargin = 100,    // Space for file explorer
    RightMargin = 300,   // Space for documentation
    TopMargin = 50,      // Space for system notifications
    BottomMargin = 100,  // Space for terminal
    AnimationDuration = 500,
    TargetApplications = { "Visual Studio", "VS Code", "JetBrains*" }
};
```

### CLI Examples
```bash
# Apply specific profile
WindowsMarginManager.exe --profile "Gaming"

# Set margins via command line
WindowsMarginManager.exe --margins 50,50,50,50 --type pixels

# Export current configuration
WindowsMarginManager.exe --export-config config.json

# Batch apply to specific applications
WindowsMarginManager.exe --filter "Chrome|Firefox" --margins 10%
```

## 🔌 **API Integration**

### REST API Endpoints
```http
GET    /api/profiles              # List all profiles
POST   /api/profiles              # Create new profile
PUT    /api/profiles/{id}         # Update profile
DELETE /api/profiles/{id}         # Delete profile
POST   /api/apply/{profileId}     # Apply profile
POST   /api/restore               # Restore windows
GET    /api/windows               # List current windows
GET    /api/monitors              # List monitors
```

### WebSocket Events
```javascript
// Real-time window events
ws.onmessage = (event) => {
    const data = JSON.parse(event.data);
    switch(data.type) {
        case 'window_moved':
        case 'profile_applied':
        case 'monitor_changed':
            // Handle events
    }
};
```

## 🎯 **Performance & Optimization**

- **Memory Efficient**: < 50MB RAM usage with thousands of windows
- **CPU Optimized**: < 1% CPU usage during normal operation
- **Battery Friendly**: Intelligent polling and event-driven architecture
- **Startup Time**: < 2 seconds cold start, < 500ms warm start
- **Response Time**: < 50ms hotkey response time

## 🛡️ **Security & Privacy**

- **No Network Access**: Completely offline operation (except optional API)
- **No Data Collection**: Zero telemetry or user tracking
- **Minimal Permissions**: Standard user privileges only
- **Open Source**: Full source code transparency
- **Secure Storage**: Encrypted configuration files

## 🔧 **Troubleshooting**

### Common Issues
- **Hotkeys Not Working**: Check for conflicts with other applications
- **Windows Not Moving**: Verify application permissions and window states
- **Performance Issues**: Adjust animation settings and polling intervals
- **Multi-Monitor Problems**: Update display drivers and check scaling settings

### Debug Mode
```bash
# Enable verbose logging
WindowsMarginManager.exe --debug --log-level verbose

# Generate diagnostic report
WindowsMarginManager.exe --diagnostics --output diagnostics.zip
```

## 🤝 **Contributing**

We welcome contributions! See [CONTRIBUTING.md](CONTRIBUTING.md) for guidelines.

### Development Setup
```bash
# Install development dependencies
dotnet restore
dotnet tool restore

# Run tests
dotnet test

# Code formatting
dotnet format
```

## 📄 **License**

MIT License - see [LICENSE](LICENSE) for details.

## 🙏 **Acknowledgments**

- Windows API documentation and community
- .NET development team
- Open source contributors
- Beta testers and feedback providers

---

**Transform your desktop experience today! 🚀**
