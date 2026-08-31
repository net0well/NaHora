import 'package:flutter/material.dart';
import 'package:flutter_map/flutter_map.dart';
import 'package:latlong2/latlong.dart';

import '../theme/app_colors.dart';
import '../theme/app_radius.dart';
import '../theme/app_spacing.dart';
import '../theme/app_typography.dart';
import 'app_location_preview.dart';

/// A position on a map, with the radius the reading was good to.
///
/// Falls back to [AppLocationPreview] whenever there is no key or no fix. That
/// is not a degraded experience to apologise for: latitude and longitude are
/// captured and sent regardless, so a tile provider being unreachable — or
/// simply unconfigured — must never look like the punch is at risk.
class AppMap extends StatelessWidget {
  const AppMap({
    required this.height,
    required this.tileKey,
    super.key,
    this.latitude,
    this.longitude,
    this.accuracyMeters,
    this.isDegraded = false,
    this.isUnavailable = false,
    this.isInteractive = false,
    this.zoom = 17,
  });

  final double height;

  /// The MapTiler key. Empty means no tiles, which is a supported state.
  final String tileKey;

  final double? latitude;
  final double? longitude;

  /// Drawn as a circle around the pin. A pin with no radius reads as a
  /// certainty the GPS never had.
  final double? accuracyMeters;

  final bool isDegraded;
  final bool isUnavailable;

  /// Panning and zooming. Off where the map sits above a button somebody is
  /// about to press and hold — a drag meant for the button would slide the map
  /// instead.
  final bool isInteractive;

  final double zoom;

  /// Dark tiles, because the app is dark and a bright rectangle in the middle
  /// of it is the only thing anybody would look at.
  static const String _style = 'streets-v2-dark';

  bool get _canDrawMap =>
      tileKey.isNotEmpty &&
      latitude != null &&
      longitude != null &&
      !isUnavailable;

  @override
  Widget build(BuildContext context) {
    if (!_canDrawMap) {
      return AppLocationPreview(
        accuracyMeters: accuracyMeters,
        height: height,
        isDegraded: isDegraded,
        isUnavailable: isUnavailable,
      );
    }

    final position = LatLng(latitude!, longitude!);

    return Semantics(
      image: true,
      label: _describe(),
      child: ExcludeSemantics(
        child: ClipRRect(
          borderRadius: BorderRadius.circular(AppRadius.md),
          child: SizedBox(
            key: const Key('app_map'),
            height: height,
            child: Stack(
              children: [
                FlutterMap(
                  options: MapOptions(
                    initialCenter: position,
                    initialZoom: zoom,
                    interactionOptions: InteractionOptions(
                      flags: isInteractive
                          ? InteractiveFlag.all & ~InteractiveFlag.rotate
                          : InteractiveFlag.none,
                    ),
                  ),
                  children: [
                    TileLayer(
                      urlTemplate:
                          'https://api.maptiler.com/maps/$_style/256/'
                          '{z}/{x}/{y}.png?key=$tileKey',
                      userAgentPackageName: 'br.com.nahora',
                      tileProvider: NetworkTileProvider(),
                    ),
                    if (accuracyMeters case final radius?)
                      CircleLayer(
                        circles: [
                          CircleMarker(
                            point: position,
                            radius: radius,
                            useRadiusInMeter: true,
                            color: AppColors.primary.withValues(alpha: 0.15),
                            borderColor: AppColors.primary.withValues(
                              alpha: 0.6,
                            ),
                            borderStrokeWidth: 1,
                          ),
                        ],
                      ),
                    MarkerLayer(
                      markers: [
                        Marker(
                          point: position,
                          width: 34,
                          height: 34,
                          child: const _Pin(key: Key('app_map_marker')),
                        ),
                      ],
                    ),
                  ],
                ),

                // Required by the tile provider's terms, and by OpenStreetMap's
                // licence for the data underneath them. Small, but not optional.
                const Align(
                  alignment: Alignment.bottomRight,
                  child: _Attribution(),
                ),
              ],
            ),
          ),
        ),
      ),
    );
  }

  String _describe() {
    final radius = accuracyMeters;

    return radius == null
        ? 'Mapa da localização'
        : 'Mapa da localização, precisão de ${radius.round()} metros';
  }
}

/// The pin itself, drawn rather than an asset so it takes the theme.
class _Pin extends StatelessWidget {
  const _Pin({super.key});

  @override
  Widget build(BuildContext context) {
    return Container(
      decoration: BoxDecoration(
        color: AppColors.primary,
        shape: BoxShape.circle,
        border: Border.all(color: AppColors.onPrimary, width: 2),
      ),
      child: const Icon(Icons.person_pin_circle, size: 20),
    );
  }
}

class _Attribution extends StatelessWidget {
  const _Attribution();

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: AppSpacing.xs),
      color: AppColors.surface.withValues(alpha: 0.7),
      child: Text(
        '© MapTiler © OpenStreetMap',
        style: AppTypography.labelSmall.copyWith(
          color: AppColors.textSecondary,
          fontSize: 9,
        ),
      ),
    );
  }
}
