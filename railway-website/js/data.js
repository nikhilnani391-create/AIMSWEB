/**
 * Structured data for all repeated content sections.
 * Edit data here instead of duplicating HTML markup.
 */

const SOLUTIONS = [
  {
    image: 'https://images.unsplash.com/photo-1474487548417-781cb71495f3?ixlib=rb-4.0.3&auto=format&fit=crop&w=800&q=80',
    imageAlt: 'Railway Technology',
    title: 'Railway Technology',
    description: 'Robust data recording and signaling solutions designed for electric and diesel locomotives, ensuring safety and compliance on every journey.',
    features: ['Electric Locomotives', 'Diesel Locomotives', 'Signal & Telecommunications'],
    linkText: 'Explore Railway Solutions',
    linkHref: '#',
  },
  {
    image: 'https://images.unsplash.com/photo-1565514020179-026b92b84bb6?ixlib=rb-4.0.3&auto=format&fit=crop&w=800&q=80',
    imageAlt: 'Industrial Automation',
    title: 'Industrial Automation',
    description: 'Advanced data logging and process visualization systems to optimize manufacturing efficiency and monitor critical infrastructure.',
    features: ['Data Recording Systems', 'Process Visualization', 'Custom Integration'],
    linkText: 'Explore Industrial Solutions',
    linkHref: '#',
  },
];

const PRODUCTS = [
  {
    image: 'https://images.unsplash.com/photo-1518770660439-4636190af475?ixlib=rb-4.0.3&auto=format&fit=crop&w=400&q=80',
    imageAlt: 'Data Logger Circuit',
    title: 'LX-9000 Data Logger',
    badgeText: 'EN 50155',
    badgeColor: 'blue',
    description: 'Multi-channel solid-state recording system optimized for high-vibration locomotive environments. Features real-time telemetry processing.',
  },
  {
    image: 'https://images.unsplash.com/photo-1551288049-bebda4e38f71?ixlib=rb-4.0.3&auto=format&fit=crop&w=400&q=80',
    imageAlt: 'HMI Touch Panel Data',
    title: 'VisuPro HMI Panel',
    badgeText: 'IP67 Rating',
    badgeColor: 'green',
    description: 'Ruggedized anti-glare touch-screen interface for real-time process visualization. Built with high-vibration resilience for industrial cabins.',
  },
  {
    image: 'https://images.unsplash.com/photo-1544197150-b99a580bb7a8?ixlib=rb-4.0.3&auto=format&fit=crop&w=400&q=80',
    imageAlt: 'Server Gateway',
    title: 'SignalSafe Gateway',
    badgeText: 'SIL 3 Certified',
    badgeColor: 'purple',
    description: 'Secure telecommunications gateway utilizing Edge AI for remote signaling, diagnostic telemetry, and predictive maintenance alerts.',
  },
];

const COMPARISON_ROWS = [
  {
    feature: 'Primary Function',
    values: ['Data Recording & Telemetry', 'Process Visualization', 'Signaling & Edge AI'],
  },
  {
    feature: 'Compliance Standard',
    values: [
      '<span class="text-blue-600 font-semibold">EN 50155</span>',
      '<span class="text-green-600 font-semibold">IP67</span>',
      '<span class="text-purple-600 font-semibold">SIL 3</span>',
    ],
  },
  {
    feature: 'Vibration Resilience',
    values: ['Extreme (Locomotive)', 'High (Industrial)', 'Moderate (Stationary)'],
  },
  {
    feature: 'Telemetry Support',
    values: ['Real-Time Wireless', 'Local Diagnostics', 'Cloud & Local Hub'],
  },
];

const MACHINERY_ITEMS = [
  {
    image: './assets/images/image1.jfif',
    imageAlt: 'Railway Track Machine',
    title: 'Track Laying Machinery',
    description: 'Automated heavy-duty track alignment and ballast regulation systems.',
  },
  {
    image: './assets/images/image2.jfif',
    imageAlt: 'Railway Crane Maintenance',
    title: 'Maintenance Cranes',
    description: 'Mobile rail-bound lifting solutions designed for overhead catenary and track maintenance.',
  },
  {
    image: './assets/images/image3.jfif',
    imageAlt: 'Railway Track Welding',
    title: 'Precision Welding Services',
    description: 'Thermite and flash-butt welding services to ensure continuous welded rails (CWR).',
  },
];

const INNOVATIONS = [
  {
    iconPath: 'M13 10V3L4 14h7v7l9-11h-7z',
    color: 'blue',
    title: 'Edge AI for Rail',
    description: 'Local processing of telemetry data instantly analyzes signal degradation and engine performance without relying on constant cloud connectivity.',
  },
  {
    iconPath: 'M9 19v-6a2 2 0 00-2-2H5a2 2 0 00-2 2v6a2 2 0 002 2h2a2 2 0 002-2zm0 0V9a2 2 0 012-2h2a2 2 0 012 2v10m-6 0a2 2 0 002 2h2a2 2 0 002-2m0 0V5a2 2 0 012-2h2a2 2 0 012 2v14a2 2 0 01-2 2h-2a2 2 0 01-2-2z',
    color: 'green',
    title: 'Predictive Maintenance',
    description: 'Advanced analytics identify anomalous vibration patterns and thermal events, alerting engineers before catastrophic hardware failure occurs.',
  },
  {
    iconPath: 'M20.618 5.984A11.955 11.955 0 0112 2.944a11.955 11.955 0 01-8.618 3.04A12.02 12.02 0 003 9c0 5.591 3.824 10.29 9 11.622 5.176-1.332 9-6.03 9-11.622 0-1.042-.133-2.052-.382-3.016zM12 9v2m0 4h.01',
    color: 'purple',
    title: 'Ruggedized Design',
    description: 'Military-grade casing, conformal coating, and shock-absorbent mounting ensure operational integrity in the harshest industrial environments.',
  },
];

const RESOURCES = [
  {
    iconPath: 'M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z',
    color: 'blue',
    title: 'Product Datasheets',
    description: 'Download comprehensive technical specifications, CAD models, and installation guides for all Laxven systems.',
    linkText: 'Access Library',
    linkHref: '#',
  },
  {
    iconPath: 'M19 11H5m14 0a2 2 0 012 2v6a2 2 0 01-2 2H5a2 2 0 01-2-2v-6a2 2 0 012-2m14 0V9a2 2 0 00-2-2M5 11V9a2 2 0 012-2m0 0V5a2 2 0 012-2h6a2 2 0 012 2v2M7 7h10',
    color: 'green',
    title: 'Case Studies',
    description: 'Read how our infrastructure upgrades have increased ROI, including our latest National Rail Fleet rollout.',
    linkText: 'Read Studies',
    linkHref: '#',
  },
  {
    iconPath: 'M18.364 5.636l-3.536 3.536m0 5.656l3.536 3.536M9.172 9.172L5.636 5.636m3.536 9.192l-3.536 3.536M21 12a9 9 0 11-18 0 9 9 0 0118 0zm-5 0a4 4 0 11-8 0 4 4 0 018 0z',
    color: 'purple',
    title: 'Technical Support Portal',
    description: 'Submit diagnostic logs, request warranty service, or chat securely with a Laxven systems engineer.',
    linkText: 'Enter Portal',
    linkHref: '#',
  },
];
