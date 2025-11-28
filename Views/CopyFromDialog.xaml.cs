using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using GTAHandler.Models;

namespace GTAHandler.Views;

public partial class CopyFromDialog : Window
{
    public VehicleHandling? SelectedVehicle { get; private set; }
    public VehicleHandling? ParsedVehicle { get; private set; }

    private readonly List<VehicleHandling> _allVehicles;
    private readonly GameType _gameType;

    public CopyFromDialog(List<VehicleHandling> vehicles, VehicleCategory category, GameType gameType)
    {
        InitializeComponent();

        _allVehicles = vehicles;
        _gameType = gameType;
        VehicleList.ItemsSource = vehicles;
        CategoryText.Text = $"Select a vehicle from {category.GetDisplayName()}:";

        VehicleList.SelectionChanged += (s, e) =>
        {
            if (VehicleList.SelectedItem != null)
            {
                // Clear parsed vehicle when selecting from list
                ParsedVehicle = null;
                UpdateCopyButtonState();
            }
        };

        // Focus raw config box
        Loaded += (s, e) => RawConfigBox.Focus();
    }

    private void UpdateCopyButtonState()
    {
        CopyButton.IsEnabled = VehicleList.SelectedItem != null || ParsedVehicle != null;
    }

    private void RawConfigBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        var text = RawConfigBox.Text?.Trim() ?? string.Empty;

        // Update placeholder visibility
        RawConfigPlaceholder.Visibility = string.IsNullOrEmpty(text)
            ? Visibility.Visible
            : Visibility.Collapsed;

        if (string.IsNullOrWhiteSpace(text))
        {
            ParsedVehicle = null;
            ParseStatusText.Visibility = Visibility.Collapsed;
            UpdateCopyButtonState();
            return;
        }

        // Try to parse the line
        try
        {
            ParsedVehicle = ParseHandlingLine(text);
            if (ParsedVehicle != null)
            {
                // Clear vehicle list selection when successfully parsing
                VehicleList.SelectedItem = null;

                ParseStatusText.Text = "✓ Config parsed successfully";
                ParseStatusText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#537b35"));
                ParseStatusText.Visibility = Visibility.Visible;
            }
        }
        catch (Exception ex)
        {
            ParsedVehicle = null;
            ParseStatusText.Text = $"✕ {ex.Message}";
            ParseStatusText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#ed1b2e"));
            ParseStatusText.Visibility = Visibility.Visible;
        }

        UpdateCopyButtonState();
    }

    private VehicleHandling? ParseHandlingLine(string line)
    {
        // Remove comments
        var commentIndex = line.IndexOf(';');
        if (commentIndex >= 0)
            line = line.Substring(0, commentIndex);

        line = line.Trim();
        if (string.IsNullOrEmpty(line))
            return null;

        // Split by whitespace
        var parts = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);

        // Validate column count based on game type
        int minColumns = _gameType switch
        {
            GameType.GTA3 => 31,
            GameType.GTAVC => 32,
            GameType.GTASA => 35,
            _ => 31
        };

        if (parts.Length < minColumns)
        {
            throw new Exception($"Expected at least {minColumns} columns for {_gameType.GetDisplayName()}, found {parts.Length}");
        }

        var vehicle = new VehicleHandling();
        vehicle.GameType = _gameType;

        try
        {
            switch (_gameType)
            {
                case GameType.GTA3:
                    ParseGTA3Line(parts, vehicle);
                    break;
                case GameType.GTAVC:
                    ParseGTAVCLine(parts, vehicle);
                    break;
                case GameType.GTASA:
                    ParseGTASALine(parts, vehicle);
                    break;
            }
        }
        catch (Exception ex)
        {
            throw new Exception($"Parse error: {ex.Message}");
        }

        return vehicle;
    }

    private void ParseGTA3Line(string[] parts, VehicleHandling v)
    {
        int i = 0;
        v.Identifier = parts[i++];
        v.Mass = ParseFloat(parts[i++]);
        v.TurnMassOrDimensionX = ParseFloat(parts[i++]);    // Dimensions.x
        v.DragMultOrDimensionY = ParseFloat(parts[i++]);    // Dimensions.y
        v.DimensionZ = ParseFloat(parts[i++]);              // Dimensions.z
        v.CentreOfMassX = ParseFloat(parts[i++]);
        v.CentreOfMassY = ParseFloat(parts[i++]);
        v.CentreOfMassZ = ParseFloat(parts[i++]);
        v.PercentSubmerged = ParseInt(parts[i++]);
        v.TractionMultiplier = ParseFloat(parts[i++]);
        v.TractionLoss = ParseFloat(parts[i++]);
        v.TractionBias = ParseFloat(parts[i++]);
        v.NumberOfGears = ParseInt(parts[i++]);
        v.MaxVelocity = ParseFloat(parts[i++]);
        v.EngineAcceleration = ParseFloat(parts[i++]);
        v.DriveType = ParseDriveType(parts[i++]);
        v.EngineType = ParseEngineType(parts[i++]);
        v.BrakeDeceleration = ParseFloat(parts[i++]);
        v.BrakeBias = ParseFloat(parts[i++]);
        v.HasABS = ParseInt(parts[i++]) != 0;
        v.SteeringLock = ParseFloat(parts[i++]);
        v.SuspensionForceLevel = ParseFloat(parts[i++]);
        v.SuspensionDampingLevel = ParseFloat(parts[i++]);
        v.SeatOffsetDistance = ParseFloat(parts[i++]);
        v.CollisionDamageMultiplier = ParseFloat(parts[i++]);
        v.MonetaryValue = ParseInt(parts[i++]);
        v.SuspensionUpperLimit = ParseFloat(parts[i++]);
        v.SuspensionLowerLimit = ParseFloat(parts[i++]);
        v.SuspensionBias = ParseFloat(parts[i++]);
        v.ModelFlags = parts[i++];
        v.FrontLights = ParseLightType(parts[i++]);
        v.RearLights = ParseLightType(parts[i++]);
    }

    private void ParseGTAVCLine(string[] parts, VehicleHandling v)
    {
        int i = 0;
        v.Identifier = parts[i++];
        v.Mass = ParseFloat(parts[i++]);
        v.TurnMassOrDimensionX = ParseFloat(parts[i++]);    // Dimensions.x
        v.DragMultOrDimensionY = ParseFloat(parts[i++]);    // Dimensions.y
        v.DimensionZ = ParseFloat(parts[i++]);              // Dimensions.z
        v.CentreOfMassX = ParseFloat(parts[i++]);
        v.CentreOfMassY = ParseFloat(parts[i++]);
        v.CentreOfMassZ = ParseFloat(parts[i++]);
        v.PercentSubmerged = ParseInt(parts[i++]);
        v.TractionMultiplier = ParseFloat(parts[i++]);
        v.TractionLoss = ParseFloat(parts[i++]);
        v.TractionBias = ParseFloat(parts[i++]);
        v.NumberOfGears = ParseInt(parts[i++]);
        v.MaxVelocity = ParseFloat(parts[i++]);
        v.EngineAcceleration = ParseFloat(parts[i++]);
        v.DriveType = ParseDriveType(parts[i++]);
        v.EngineType = ParseEngineType(parts[i++]);
        v.BrakeDeceleration = ParseFloat(parts[i++]);
        v.BrakeBias = ParseFloat(parts[i++]);
        v.HasABS = ParseInt(parts[i++]) != 0;
        v.SteeringLock = ParseFloat(parts[i++]);
        v.SuspensionForceLevel = ParseFloat(parts[i++]);
        v.SuspensionDampingLevel = ParseFloat(parts[i++]);
        v.SeatOffsetDistance = ParseFloat(parts[i++]);
        v.CollisionDamageMultiplier = ParseFloat(parts[i++]);
        v.MonetaryValue = ParseInt(parts[i++]);
        v.SuspensionUpperLimit = ParseFloat(parts[i++]);
        v.SuspensionLowerLimit = ParseFloat(parts[i++]);
        v.SuspensionBias = ParseFloat(parts[i++]);
        v.SuspensionAntiDiveMultiplier = ParseFloat(parts[i++]);  // VC has this
        v.ModelFlags = parts[i++];
        v.FrontLights = ParseLightType(parts[i++]);
        v.RearLights = ParseLightType(parts[i++]);
    }

    private void ParseGTASALine(string[] parts, VehicleHandling v)
    {
        int i = 0;
        v.Identifier = parts[i++];
        v.Mass = ParseFloat(parts[i++]);
        v.TurnMassOrDimensionX = ParseFloat(parts[i++]);    // fTurnMass
        v.DragMultOrDimensionY = ParseFloat(parts[i++]);    // fDragMult
        v.CentreOfMassX = ParseFloat(parts[i++]);
        v.CentreOfMassY = ParseFloat(parts[i++]);
        v.CentreOfMassZ = ParseFloat(parts[i++]);
        v.PercentSubmerged = ParseInt(parts[i++]);
        v.TractionMultiplier = ParseFloat(parts[i++]);
        v.TractionLoss = ParseFloat(parts[i++]);
        v.TractionBias = ParseFloat(parts[i++]);
        v.NumberOfGears = ParseInt(parts[i++]);
        v.MaxVelocity = ParseFloat(parts[i++]);
        v.EngineAcceleration = ParseFloat(parts[i++]);
        v.EngineInertia = ParseFloat(parts[i++]);           // SA-specific
        v.DriveType = ParseDriveType(parts[i++]);
        v.EngineType = ParseEngineType(parts[i++]);
        v.BrakeDeceleration = ParseFloat(parts[i++]);
        v.BrakeBias = ParseFloat(parts[i++]);
        v.HasABS = ParseInt(parts[i++]) != 0;
        v.SteeringLock = ParseFloat(parts[i++]);
        v.SuspensionForceLevel = ParseFloat(parts[i++]);
        v.SuspensionDampingLevel = ParseFloat(parts[i++]);
        v.SuspensionHighSpeedComDamp = ParseFloat(parts[i++]);  // SA-specific
        v.SuspensionUpperLimit = ParseFloat(parts[i++]);
        v.SuspensionLowerLimit = ParseFloat(parts[i++]);
        v.SuspensionBias = ParseFloat(parts[i++]);
        v.SuspensionAntiDiveMultiplier = ParseFloat(parts[i++]);
        v.SeatOffsetDistance = ParseFloat(parts[i++]);
        v.CollisionDamageMultiplier = ParseFloat(parts[i++]);
        v.MonetaryValue = ParseInt(parts[i++]);
        v.ModelFlags = parts[i++];
        v.HandlingFlags = parts[i++];                       // SA-specific
        v.FrontLights = ParseLightType(parts[i++]);
        v.RearLights = ParseLightType(parts[i++]);
        if (i < parts.Length)
        {
            v.AnimGroup = ParseInt(parts[i]);               // SA-specific
        }
    }

    private float ParseFloat(string s) => float.Parse(s, CultureInfo.InvariantCulture);
    private int ParseInt(string s) => int.Parse(s, CultureInfo.InvariantCulture);

    private DriveType ParseDriveType(string s) => s.ToUpper() switch
    {
        "F" => DriveType.Front,
        "R" => DriveType.Rear,
        "4" => DriveType.FourWheel,
        _ => DriveType.Rear
    };

    private EngineType ParseEngineType(string s) => s.ToUpper() switch
    {
        "P" => EngineType.Petrol,
        "D" => EngineType.Diesel,
        "E" => EngineType.Electric,
        _ => EngineType.Petrol
    };

    private LightType ParseLightType(string s) => int.Parse(s) switch
    {
        0 => LightType.Long,
        1 => LightType.Small,
        2 => LightType.Big,
        3 => LightType.Tall,
        _ => LightType.Long
    };

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        var searchText = SearchBox.Text?.ToLowerInvariant() ?? string.Empty;

        // Update placeholder visibility
        SearchPlaceholder.Visibility = string.IsNullOrEmpty(SearchBox.Text)
            ? Visibility.Visible
            : Visibility.Collapsed;

        // Filter vehicles
        if (string.IsNullOrWhiteSpace(searchText))
        {
            VehicleList.ItemsSource = _allVehicles;
        }
        else
        {
            VehicleList.ItemsSource = _allVehicles
                .Where(v => v.Identifier.ToLowerInvariant().Contains(searchText))
                .ToList();
        }
    }

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 1)
        {
            DragMove();
        }
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void CopyButton_Click(object sender, RoutedEventArgs e)
    {
        // Prefer parsed vehicle over selected
        if (ParsedVehicle != null)
        {
            SelectedVehicle = ParsedVehicle;
        }
        else
        {
            SelectedVehicle = VehicleList.SelectedItem as VehicleHandling;
        }

        DialogResult = true;
        Close();
    }

    private void VehicleList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (VehicleList.SelectedItem != null)
        {
            SelectedVehicle = VehicleList.SelectedItem as VehicleHandling;
            DialogResult = true;
            Close();
        }
    }
}

