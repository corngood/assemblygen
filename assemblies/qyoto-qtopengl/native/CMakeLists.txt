find_package(Qt4 REQUIRED COMPONENTS QtCore QtOpenGL)
find_package(Smoke REQUIRED COMPONENTS QtOpenGL)

# source files
SET(SRC_QYOTO_QTOPENGL_NATIVE
    qtopengl.cpp
)

INCLUDE_DIRECTORIES (${QT_INCLUDES} ${SMOKE_INCLUDE_DIR} ${SMOKE_QTOPENGL_INCLUDE_DIR}/smoke ../../qyoto-qtcore/native)

ADD_LIBRARY (qyoto-qtopengl-native SHARED ${SRC_QYOTO_QTOPENGL_NATIVE})
TARGET_LINK_LIBRARIES (qyoto-qtopengl-native qyoto-qtcore-native ${SMOKE_BASE_LIBRARY} ${SMOKE_QTOPENGL_LIBRARY} ${QT_QTCORE_LIBRARY} ${QT_QTOPENGL_LIBRARY})
set_target_properties(qyoto-qtopengl-native PROPERTIES VERSION 2.0.0 SOVERSION 2)

INSTALL(TARGETS qyoto-qtopengl-native LIBRARY DESTINATION lib)