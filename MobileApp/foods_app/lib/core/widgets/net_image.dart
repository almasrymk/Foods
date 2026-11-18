import 'package:cached_network_image/cached_network_image.dart';
import 'package:material_ui/material_ui.dart';

import '../config/app_config.dart';
import '../theme/app_colors.dart';

/// An image from the server with a placeholder and a local fallback.
class NetImage extends StatelessWidget {
  const NetImage(this.path, {super.key, this.fit = BoxFit.cover, this.fallbackAsset, this.width, this.height, this.radius});

  final String? path;
  final BoxFit fit;
  final String? fallbackAsset;
  final double? width;
  final double? height;
  final BorderRadius? radius;

  @override
  Widget build(BuildContext context) {
    final url = AppConfig.mediaUrl(path);
    Widget fallback() => fallbackAsset != null
        ? Image.asset(fallbackAsset!, fit: fit, width: width, height: height)
        : Container(
            width: width,
            height: height,
            color: AppColors.backgroundWarm,
            alignment: Alignment.center,
            child: const Icon(Icons.restaurant, color: AppColors.hint),
          );
    final Widget child = url.isEmpty
        ? fallback()
        : CachedNetworkImage(
            imageUrl: url,
            fit: fit,
            width: width,
            height: height,
            fadeInDuration: const Duration(milliseconds: 150),
            placeholder: (_, _) => Container(width: width, height: height, color: AppColors.backgroundWarm),
            errorWidget: (_, _, _) => fallback(),
          );
    return radius == null ? child : ClipRRect(borderRadius: radius!, child: child);
  }
}


/// Swipeable photo album that fills its box exactly (like `fa-photo-slider`):
/// one photo is a plain [NetImage]; more get a PageView and dots sized to the box,
/// so the dots never spill outside a small thumbnail.
class PhotoSlider extends StatefulWidget {
  const PhotoSlider({super.key, required this.images, this.fallback, this.fit = BoxFit.cover, this.fallbackAsset});
  final List<String> images;
  final String? fallback;
  final BoxFit fit;
  final String? fallbackAsset;

  @override
  State<PhotoSlider> createState() => _PhotoSliderState();
}

class _PhotoSliderState extends State<PhotoSlider> {
  int _index = 0;

  @override
  Widget build(BuildContext context) {
    final seen = <String>{};
    final urls = [for (final p in widget.images) if (p.trim().isNotEmpty && seen.add(p.trim())) p.trim()];
    if (urls.length <= 1) {
      return NetImage(urls.firstOrNull ?? widget.fallback, fit: widget.fit, fallbackAsset: widget.fallbackAsset);
    }
    return LayoutBuilder(
      builder: (context, box) {
        final small = box.maxWidth < 140;
        final gap = small ? 2.0 : 4.0;
        final pad = small ? 2.0 : 5.0;
        // Shrink the dots so the strip always fits inside the photo.
        final room = box.maxWidth - 6 - pad * 2 - gap * (urls.length - 1);
        final dot = (room / urls.length).clamp(2.0, small ? 3.5 : 6.0);
        return Stack(
          children: [
            Positioned.fill(
              child: PageView(
                onPageChanged: (i) => setState(() => _index = i),
                children: [for (final u in urls) NetImage(u, fit: widget.fit, fallbackAsset: widget.fallbackAsset)],
              ),
            ),
            Positioned(
              left: 0,
              right: 0,
              bottom: small ? 2 : 6,
              child: Center(
                child: Container(
                  padding: EdgeInsets.symmetric(horizontal: pad, vertical: small ? 1 : 3),
                  decoration: BoxDecoration(color: const Color(0x47000000), borderRadius: BorderRadius.circular(99)),
                  child: Row(
                    mainAxisSize: MainAxisSize.min,
                    textDirection: TextDirection.ltr,
                    children: [
                      for (var i = 0; i < urls.length; i++) ...[
                        if (i > 0) SizedBox(width: gap),
                        Container(
                          width: i == _index ? dot * 1.25 : dot,
                          height: i == _index ? dot * 1.25 : dot,
                          decoration: BoxDecoration(
                            shape: BoxShape.circle,
                            color: i == _index ? Colors.white : const Color(0x99FFFFFF),
                          ),
                        ),
                      ],
                    ],
                  ),
                ),
              ),
            ),
          ],
        );
      },
    );
  }
}

/// A photo (or album) shown at its own proportions: the box takes the full width it is
/// given and the height that keeps the first picture's aspect ratio, so nothing is cropped.
class NaturalPhoto extends StatefulWidget {
  const NaturalPhoto({super.key, required this.images, this.fallbackAsset, this.radius, this.minRatio = .9, this.maxRatio = 2.4, this.maxHeight, this.whole = false});
  /// Always show the whole picture (every photo of the album, none cropped).
  final bool whole;
  /// The photo is never taller than this; it gets narrower (centred) instead.
  final double? maxHeight;
  final List<String> images;
  final String? fallbackAsset;
  final BorderRadius? radius;
  /// Width ÷ height limits, so a very tall or very wide picture still fits the screen.
  final double minRatio;
  final double maxRatio;

  @override
  State<NaturalPhoto> createState() => _NaturalPhotoState();
}

class _NaturalPhotoState extends State<NaturalPhoto> {
  double? _ratio;
  ImageStream? _stream;
  ImageStreamListener? _listener;
  String _resolved = '';

  String get _first => widget.images.map((e) => e.trim()).firstWhere((e) => e.isNotEmpty, orElse: () => '');

  @override
  void didChangeDependencies() {
    super.didChangeDependencies();
    _resolve();
  }

  @override
  void didUpdateWidget(covariant NaturalPhoto old) {
    super.didUpdateWidget(old);
    _resolve();
  }

  void _resolve() {
    final first = _first;
    final key = first.isEmpty ? (widget.fallbackAsset ?? '') : first;
    if (key == _resolved) return;
    _resolved = key;
    _stop();
    final url = AppConfig.mediaUrl(first);
    final ImageProvider? provider = url.isNotEmpty
        ? CachedNetworkImageProvider(url)
        : (widget.fallbackAsset != null ? AssetImage(widget.fallbackAsset!) : null);
    if (provider == null) return;
    final stream = provider.resolve(createLocalImageConfiguration(context));
    final listener = ImageStreamListener(
      (info, _) {
        final h = info.image.height;
        if (h > 0 && mounted) setState(() => _ratio = info.image.width / h);
      },
      onError: (_, _) {},
    );
    stream.addListener(listener);
    _stream = stream;
    _listener = listener;
  }

  void _stop() {
    if (_stream != null && _listener != null) _stream!.removeListener(_listener!);
    _stream = null;
    _listener = null;
  }

  @override
  void dispose() {
    _stop();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final natural = _ratio ?? 16 / 10;
    final ratio = natural.clamp(widget.minRatio, widget.maxRatio);
    // A very tall or very wide picture is shown whole inside the capped box, never cropped.
    final capped = (ratio - natural).abs() > .01;
    final Widget photo = capped || widget.whole
        ? ColoredBox(
            color: const Color(0xFFF6EDE3),
            child: PhotoSlider(images: widget.images, fit: BoxFit.contain, fallbackAsset: widget.fallbackAsset),
          )
        : PhotoSlider(images: widget.images, fit: BoxFit.cover, fallbackAsset: widget.fallbackAsset);
    final framed = widget.radius == null ? photo : ClipRRect(borderRadius: widget.radius!, child: photo);
    final maxH = widget.maxHeight;
    if (maxH == null) {
      return AnimatedSize(duration: const Duration(milliseconds: 180), child: AspectRatio(aspectRatio: ratio, child: framed));
    }
    return LayoutBuilder(
      builder: (context, box) {
        final w = box.maxWidth.isFinite ? (box.maxWidth < maxH * ratio ? box.maxWidth : maxH * ratio) : maxH * ratio;
        return AnimatedSize(
          duration: const Duration(milliseconds: 180),
          child: Center(child: SizedBox(width: w, height: w / ratio, child: framed)),
        );
      },
    );
  }
}

/// Zooms a dish or ingredient picture: it grows a little while the mouse is over it, and a
/// long press (or a tap when [tapToOpen]) opens it full screen, where it can be pinched.
class FaZoom extends StatefulWidget {
  const FaZoom({super.key, required this.images, required this.child, this.fallbackAsset, this.tapToOpen = false, this.hoverScale = 1.12});
  final List<String> images;
  final Widget child;
  final String? fallbackAsset;
  final bool tapToOpen;
  final double hoverScale;

  @override
  State<FaZoom> createState() => _FaZoomState();
}

class _FaZoomState extends State<FaZoom> {
  bool _hover = false;

  void _open() => showPhotoViewer(context, widget.images, fallbackAsset: widget.fallbackAsset);

  @override
  Widget build(BuildContext context) {
    return MouseRegion(
      onEnter: (_) => setState(() => _hover = true),
      onExit: (_) => setState(() => _hover = false),
      child: GestureDetector(
        behavior: HitTestBehavior.opaque,
        onLongPress: _open,
        onTap: widget.tapToOpen ? _open : null,
        child: AnimatedScale(
          scale: _hover ? widget.hoverScale : 1,
          duration: const Duration(milliseconds: 160),
          curve: Curves.easeOut,
          child: widget.child,
        ),
      ),
    );
  }
}

/// Full-screen photo viewer: swipe between photos, pinch or double-tap to zoom, tap ✕ to close.
Future<void> showPhotoViewer(BuildContext context, List<String> images, {String? fallbackAsset}) {
  final seen = <String>{};
  final urls = [for (final p in images) if (p.trim().isNotEmpty && seen.add(p.trim())) p.trim()];
  return showGeneralDialog(
    context: context,
    barrierDismissible: true,
    barrierLabel: 'إغلاق',
    barrierColor: const Color(0xE6000000),
    transitionDuration: const Duration(milliseconds: 160),
    pageBuilder: (ctx, _, _) => _PhotoViewer(urls: urls, fallbackAsset: fallbackAsset),
    transitionBuilder: (_, anim, _, child) => FadeTransition(
      opacity: anim,
      child: ScaleTransition(scale: Tween(begin: .92, end: 1.0).animate(anim), child: child),
    ),
  );
}

class _PhotoViewer extends StatefulWidget {
  const _PhotoViewer({required this.urls, this.fallbackAsset});
  final List<String> urls;
  final String? fallbackAsset;

  @override
  State<_PhotoViewer> createState() => _PhotoViewerState();
}

class _PhotoViewerState extends State<_PhotoViewer> {
  int _index = 0;

  Widget _page(String? url) => InteractiveViewer(
    minScale: 1,
    maxScale: 4,
    child: Center(child: NetImage(url, fit: BoxFit.contain, fallbackAsset: widget.fallbackAsset)),
  );

  @override
  Widget build(BuildContext context) {
    final urls = widget.urls;
    return SafeArea(
      child: Stack(
        children: [
          Positioned.fill(
            child: GestureDetector(
              onTap: () => Navigator.of(context).pop(),
              child: urls.length <= 1
                  ? _page(urls.firstOrNull)
                  : PageView(
                      onPageChanged: (i) => setState(() => _index = i),
                      children: [for (final u in urls) _page(u)],
                    ),
            ),
          ),
          Positioned(
            top: 12,
            right: 12,
            child: Material(
              color: const Color(0x33FFFFFF),
              shape: const CircleBorder(),
              child: IconButton(
                onPressed: () => Navigator.of(context).pop(),
                icon: const Icon(Icons.close, color: Colors.white),
                tooltip: 'إغلاق',
              ),
            ),
          ),
          if (urls.length > 1)
            Positioned(
              left: 0,
              right: 0,
              bottom: 18,
              child: Center(
                child: Text(
                  '${_index + 1} / ${urls.length}',
                  textDirection: TextDirection.ltr,
                  style: const TextStyle(color: Colors.white, fontSize: 15, fontWeight: FontWeight.w700),
                ),
              ),
            ),
        ],
      ),
    );
  }
}
