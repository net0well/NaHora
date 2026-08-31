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
class AppMap extends StatefulWidget {
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

  /// Panning and zooming, plus the control that undoes them.
  ///
  /// Moving the map never moves the mark: the pin is bound to the coordinate,
  /// not to whatever happens to be in the middle of the screen.
  final bool isInteractive;

  final double zoom;

  @override
  State<AppMap> createState() => _AppMapState();
}

class _AppMapState extends State<AppMap> {
  /// Dark tiles, because the app is dark and a bright rectangle in the middle
  /// of it is the only thing anybody would look at.
  static const String _style = 'streets-v2-dark';

  /// How far the view may drift before the way back is offered. Small enough
  /// that a deliberate pan reveals it, large enough that the pixel snapping of
  /// a settling gesture does not.
  static const double _driftMeters = 30;

  final MapController _controller = MapController();

  bool _hasDrifted = false;

  LatLng? get _point => widget.latitude == null || widget.longitude == null
      ? null
      : LatLng(widget.latitude!, widget.longitude!);

  bool get _canDrawMap => _canDraw(widget);

  static bool _canDraw(AppMap widget) =>
      widget.tileKey.isNotEmpty &&
      widget.latitude != null &&
      widget.longitude != null &&
      !widget.isUnavailable;

  @override
  void didUpdateWidget(AppMap oldWidget) {
    super.didUpdateWidget(oldWidget);

    final point = _point;

    // Only when a map was already on screen. Coming from the fallback there is
    // no map yet, and the controller throws if it is driven before one has
    // rendered — the fresh map opens on the new point anyway.
    if (!_canDraw(oldWidget)) {
      return;
    }

    // A newer fix recentres the map — unless the person moved it themselves.
    // Yanking the view back under their thumb is the thing the recentre button
    // exists to let them ask for.
    if (point != null && !_hasDrifted && _movedFrom(oldWidget)) {
      _controller.move(point, widget.zoom);
    }
  }

  @override
  void dispose() {
    _controller.dispose();
    super.dispose();
  }

  bool _movedFrom(AppMap oldWidget) =>
      oldWidget.latitude != widget.latitude ||
      oldWidget.longitude != widget.longitude;

  void _onPositionChanged(MapCamera camera, bool hasGesture) {
    if (!hasGesture || !widget.isInteractive) {
      return;
    }

    final point = _point;
    if (point == null) {
      return;
    }

    final drifted =
        const Distance().as(LengthUnit.Meter, camera.center, point) >
        _driftMeters;

    if (drifted != _hasDrifted) {
      setState(() => _hasDrifted = drifted);
    }
  }

  void _recentre() {
    final point = _point;
    if (point == null) {
      return;
    }

    _controller.move(point, widget.zoom);
    setState(() => _hasDrifted = false);
  }

  @override
  Widget build(BuildContext context) {
    if (!_canDrawMap) {
      return AppLocationPreview(
        accuracyMeters: widget.accuracyMeters,
        height: widget.height,
        isDegraded: widget.isDegraded,
        isUnavailable: widget.isUnavailable,
      );
    }

    final point = _point!;

    // The label sits on the map layer alone rather than on everything: the
    // recentre button on top of it has to stay reachable, and a screen reader
    // that only ever hears "map" cannot press it.
    return ClipRRect(
      borderRadius: BorderRadius.circular(AppRadius.md),
      child: SizedBox(
        key: const Key('app_map'),
        height: widget.height,
        child: Stack(
          children: [
            Semantics(
              image: true,
              label: _describe(),
              child: ExcludeSemantics(
                child: FlutterMap(
                  mapController: _controller,
                  options: MapOptions(
                    initialCenter: point,
                    initialZoom: widget.zoom,
                    onPositionChanged: _onPositionChanged,
                    interactionOptions: InteractionOptions(
                      // Rotation off in both modes: a map turned by accident
                      // is a map somebody has to work out how to straighten.
                      flags: widget.isInteractive
                          ? InteractiveFlag.all & ~InteractiveFlag.rotate
                          : InteractiveFlag.none,
                    ),
                  ),
                  children: [
                    TileLayer(
                      urlTemplate:
                          'https://api.maptiler.com/maps/$_style/256/'
                          '{z}/{x}/{y}.png?key=${widget.tileKey}',
                      userAgentPackageName: 'br.com.nahora',
                      tileProvider: NetworkTileProvider(),
                    ),
                    if (widget.accuracyMeters case final radius?)
                      CircleLayer(
                        circles: [
                          CircleMarker(
                            point: point,
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
                          point: point,
                          width: 34,
                          height: 34,
                          child: const _Pin(key: Key('app_map_marker')),
                        ),
                      ],
                    ),
                  ],
                ),
              ),
            ),

            if (_hasDrifted)
              Positioned(
                top: AppSpacing.sm,
                right: AppSpacing.sm,
                child: _Recentre(onPressed: _recentre),
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
    );
  }

  String _describe() {
    final radius = widget.accuracyMeters;

    return radius == null
        ? 'Mapa da localização'
        : 'Mapa da localização, precisão de ${radius.round()} metros';
  }
}

/// The way back to the mark after the map has been moved.
///
/// Only appears once the view has actually drifted. A control that is always
/// there is one more thing to read on a screen whose job is a single decision.
class _Recentre extends StatelessWidget {
  const _Recentre({required this.onPressed});

  final VoidCallback onPressed;

  @override
  Widget build(BuildContext context) {
    return Semantics(
      button: true,
      label: 'Voltar para o ponto da marcação',
      onTap: onPressed,
      child: ExcludeSemantics(
        child: Material(
          key: const Key('app_map_recentre'),
          color: AppColors.surface,
          shape: const CircleBorder(side: BorderSide(color: AppColors.border)),
          clipBehavior: Clip.antiAlias,
          child: InkWell(
            onTap: onPressed,
            child: const Padding(
              padding: EdgeInsets.all(AppSpacing.sm),
              child: Icon(
                Icons.my_location,
                size: 20,
                color: AppColors.primary,
              ),
            ),
          ),
        ),
      ),
    );
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
