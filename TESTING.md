# Windows Margin Manager Pro - Testing Guide

## Testing Environment Requirements

This application requires a Windows environment for testing due to:
- Windows Forms UI framework
- Windows-specific APIs (user32.dll, kernel32.dll)
- Windows Management Instrumentation (WMI) for system event monitoring
- Windows display APIs for multi-monitor support

## Comprehensive Testing Checklist

### ✅ Core Functionality Testing
- [ ] **Hotkey Registration**: Verify Ctrl+Alt+M registers and triggers margin application
- [ ] **Window Resizing**: Confirm all top-level windows resize with configured margins
- [ ] **Margin Calculation**: Test pixel, percentage, and adaptive margin modes
- [ ] **Window Restoration**: Verify Ctrl+Alt+R restores original window positions
- [ ] **System Tray Integration**: Confirm application runs minimized in system tray

### ✅ Multi-Monitor Support Testing (318 lines implemented)
- [ ] **Per-Monitor Configuration**: Test different margin settings for each display
- [ ] **DPI Scaling**: Verify proper scaling on high-DPI monitors
- [ ] **Monitor Detection**: Confirm automatic detection of connected/disconnected monitors
- [ ] **Resolution Handling**: Test behavior with different monitor resolutions
- [ ] **Primary/Secondary Monitor**: Verify correct handling of monitor roles

### ✅ Advanced Window Filtering Testing (483 lines implemented)
- [ ] **Application Filtering**: Test inclusion/exclusion by process name
- [ ] **Title Pattern Matching**: Verify regex and wildcard title filtering
- [ ] **Window Class Filtering**: Test filtering by window class names
- [ ] **Size-Based Filtering**: Verify minimum/maximum area and aspect ratio filters
- [ ] **Time-Based Filtering**: Test time range and day-of-week restrictions
- [ ] **Filter Presets**: Verify built-in filter configurations work correctly

### ✅ Animation Effects Testing (591 lines implemented)
- [ ] **Easing Functions**: Test all 27 easing types (Linear, Quad, Cubic, etc.)
- [ ] **Animation Duration**: Verify configurable timing (100ms to 2000ms)
- [ ] **Performance Monitoring**: Check animation statistics and frame rates
- [ ] **Smooth Transitions**: Confirm fluid window movement without stuttering
- [ ] **Animation Presets**: Test predefined animation configurations

### ✅ Profile Management Testing (665 lines implemented)
- [ ] **Profile Creation**: Test saving current margin configuration as profile
- [ ] **Profile Loading**: Verify switching between saved profiles
- [ ] **Hotkey Switching**: Test Ctrl+Alt+1-9 profile hotkeys
- [ ] **Import/Export**: Verify profile sharing functionality
- [ ] **Profile Persistence**: Confirm profiles survive application restart

### ✅ Scheduling System Testing (1,630 lines implemented)
- [ ] **Time-Based Triggers**: Test specific time, daily, weekly, and interval schedules
- [ ] **Event-Based Triggers**: Verify application launch and system event triggers
- [ ] **Task Persistence**: Confirm scheduled tasks survive application restart
- [ ] **Background Execution**: Test task execution when application is minimized
- [ ] **Task Management UI**: Verify create, edit, delete, enable/disable functionality

### ✅ Smart Positioning Testing (1,705 lines implemented)
- [ ] **Usage Pattern Learning**: Test window placement based on historical usage
- [ ] **Golden Ratio Positioning**: Verify mathematically aesthetic window layouts
- [ ] **Content-Aware Positioning**: Test application-specific positioning strategies
- [ ] **Collision Avoidance**: Verify windows don't overlap when positioned
- [ ] **Adaptive Margins**: Test dynamic margin calculation based on context
- [ ] **Algorithm Selection**: Verify weighted scoring selects optimal positioning

### ✅ User Interface Testing
- [ ] **Settings Forms**: Test all configuration dialogs and forms
- [ ] **Tray Menu**: Verify all context menu items function correctly
- [ ] **Keyboard Navigation**: Test tab order and keyboard accessibility
- [ ] **Error Handling**: Verify graceful handling of invalid configurations
- [ ] **Help Documentation**: Test access to help and about information

### ✅ Performance Testing
- [ ] **Memory Usage**: Monitor memory consumption with many windows
- [ ] **CPU Usage**: Verify low CPU usage during idle and active periods
- [ ] **Response Time**: Test hotkey response time under various loads
- [ ] **Startup Time**: Measure application startup and initialization time
- [ ] **Multi-Monitor Performance**: Test performance with multiple displays

### ✅ Integration Testing
- [ ] **Windows Compatibility**: Test on Windows 10 and Windows 11
- [ ] **Application Compatibility**: Test with various applications (browsers, editors, games)
- [ ] **System Event Integration**: Verify proper handling of system sleep/wake cycles
- [ ] **User Session Changes**: Test behavior during user login/logout
- [ ] **Display Configuration Changes**: Test monitor connect/disconnect scenarios

## Test Data and Scenarios

### Sample Applications for Testing
- **Browsers**: Chrome, Firefox, Edge
- **Code Editors**: Visual Studio Code, Notepad++, Visual Studio
- **Media Players**: VLC, Windows Media Player, Spotify
- **Productivity**: Microsoft Office, Adobe Creative Suite
- **Games**: Steam games, Windows Store games
- **Utilities**: Calculator, Notepad, Command Prompt

### Multi-Monitor Test Configurations
- **Dual Monitor**: 1920x1080 + 1920x1080
- **Mixed Resolution**: 1920x1080 + 2560x1440
- **High DPI**: 4K monitor with 150% scaling
- **Ultrawide**: 3440x1440 ultrawide monitor
- **Vertical**: Portrait orientation monitor

### Performance Benchmarks
- **Memory Usage**: < 50MB idle, < 100MB active
- **CPU Usage**: < 1% idle, < 5% during animations
- **Response Time**: < 100ms hotkey response
- **Startup Time**: < 3 seconds cold start

## Automated Testing Considerations

While this application requires manual testing due to its Windows-specific nature, consider implementing:
- Unit tests for core algorithms and calculations
- Integration tests for configuration management
- Performance benchmarks for animation and positioning
- Automated UI tests using Windows Application Driver

## Known Limitations

- Requires Windows 10 or later
- Some applications may not respond to programmatic resizing
- Fullscreen applications (games) may override margin settings
- High-DPI scaling may require additional testing
- Some antivirus software may flag hotkey registration as suspicious

## Testing Sign-off

- [ ] All core functionality tests passed
- [ ] Multi-monitor support verified
- [ ] Advanced features tested and working
- [ ] Performance benchmarks met
- [ ] No critical bugs identified
- [ ] Ready for production deployment

**Tester**: ________________  
**Date**: ________________  
**Environment**: ________________  
**Notes**: ________________
